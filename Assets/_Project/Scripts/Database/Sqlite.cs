using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LastSignal.Database
{
    /// <summary>
    /// Minimal SQLite binding over the native library (macOS ships libsqlite3; Windows needs sqlite3.dll in
    /// Plugins). Only parameterised statements are exposed: values are always bound with sqlite3_bind_*,
    /// never concatenated into SQL.
    /// </summary>
    internal static class SqliteNative
    {
        private const string Lib = "sqlite3";

        public const int OK = 0, ROW = 100, DONE = 101;
        public const int OPEN_READWRITE = 0x2, OPEN_CREATE = 0x4, OPEN_FULLMUTEX = 0x10000;
        public const int INTEGER = 1, FLOAT = 2, TEXT = 3, BLOB = 4, NULL = 5;
        public static readonly IntPtr TRANSIENT = new(-1);

        [DllImport(Lib)] public static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
        [DllImport(Lib)] public static extern int sqlite3_close_v2(IntPtr db);
        [DllImport(Lib)] public static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int nBytes, out IntPtr stmt, IntPtr tail);
        [DllImport(Lib)] public static extern int sqlite3_step(IntPtr stmt);
        [DllImport(Lib)] public static extern int sqlite3_finalize(IntPtr stmt);
        [DllImport(Lib)] public static extern int sqlite3_bind_parameter_index(IntPtr stmt, byte[] name);
        [DllImport(Lib)] public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);
        [DllImport(Lib)] public static extern int sqlite3_bind_double(IntPtr stmt, int index, double value);
        [DllImport(Lib)] public static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int n, IntPtr destructor);
        [DllImport(Lib)] public static extern int sqlite3_bind_null(IntPtr stmt, int index);
        [DllImport(Lib)] public static extern int sqlite3_column_count(IntPtr stmt);
        [DllImport(Lib)] public static extern IntPtr sqlite3_column_name(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern int sqlite3_column_type(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern long sqlite3_column_int64(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern double sqlite3_column_double(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern IntPtr sqlite3_column_text(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern int sqlite3_column_bytes(IntPtr stmt, int col);
        [DllImport(Lib)] public static extern IntPtr sqlite3_errmsg(IntPtr db);
        [DllImport(Lib)] public static extern long sqlite3_last_insert_rowid(IntPtr db);
        [DllImport(Lib)] public static extern int sqlite3_changes(IntPtr db);

        public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s + "\0");

        public static string PtrToUtf8(IntPtr p, int length = -1)
        {
            if (p == IntPtr.Zero) return null;
            if (length < 0)
            {
                length = 0;
                while (Marshal.ReadByte(p, length) != 0) length++;
            }
            var bytes = new byte[length];
            Marshal.Copy(p, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public sealed class SqliteException : Exception
    {
        public int Code { get; }
        public SqliteException(int code, string message) : base($"SQLite error {code}: {message}") => Code = code;
    }

    /// <summary>One row of a query result: values by column name (long, double, string or null).</summary>
    public sealed class SqliteRow
    {
        private readonly Dictionary<string, object> _values;
        internal SqliteRow(Dictionary<string, object> values, object first)
        {
            _values = values;
            First = first;
        }

        /// <summary>Value of the first column (for scalar queries).</summary>
        public object First { get; }

        public object this[string column] => _values.TryGetValue(column, out var v) ? v : null;
        public long Long(string column) => this[column] is long l ? l : this[column] is double d ? (long)d : 0;
        public int Int(string column) => (int)Long(column);
        public bool Bool(string column) => Long(column) != 0;
        public string Text(string column) => this[column]?.ToString();
        public bool IsNull(string column) => this[column] == null;
    }

    /// <summary>
    /// A connection to one database file. Parameters are passed as (":name", value) pairs and bound natively.
    /// </summary>
    public sealed class SqliteConnection : IDisposable
    {
        private IntPtr _db;

        public string Path { get; }
        public bool IsOpen => _db != IntPtr.Zero;

        public SqliteConnection(string path)
        {
            Path = path;
            int rc = SqliteNative.sqlite3_open_v2(SqliteNative.Utf8(path), out _db,
                SqliteNative.OPEN_READWRITE | SqliteNative.OPEN_CREATE | SqliteNative.OPEN_FULLMUTEX, IntPtr.Zero);
            if (rc != SqliteNative.OK)
            {
                var msg = _db != IntPtr.Zero ? Error() : "cannot open database";
                SqliteNative.sqlite3_close_v2(_db);
                _db = IntPtr.Zero;
                throw new SqliteException(rc, $"{msg} ({path})");
            }
            Execute("PRAGMA foreign_keys = ON;");
        }

        /// <summary>Runs a statement that returns no rows. Returns the number of changed rows.</summary>
        public int Execute(string sql, params (string name, object value)[] args)
        {
            var stmt = Prepare(sql, args);
            try
            {
                int rc;
                while ((rc = SqliteNative.sqlite3_step(stmt)) == SqliteNative.ROW) { }
                if (rc != SqliteNative.DONE) throw new SqliteException(rc, Error());
                return SqliteNative.sqlite3_changes(_db);
            }
            finally
            {
                SqliteNative.sqlite3_finalize(stmt);
            }
        }

        public List<SqliteRow> Query(string sql, params (string name, object value)[] args)
        {
            var rows = new List<SqliteRow>();
            var stmt = Prepare(sql, args);
            try
            {
                int columns = SqliteNative.sqlite3_column_count(stmt);
                var names = new string[columns];
                for (int c = 0; c < columns; c++) names[c] = SqliteNative.PtrToUtf8(SqliteNative.sqlite3_column_name(stmt, c));
                int rc;
                while ((rc = SqliteNative.sqlite3_step(stmt)) == SqliteNative.ROW)
                {
                    var values = new Dictionary<string, object>(columns, StringComparer.OrdinalIgnoreCase);
                    object first = null;
                    for (int c = 0; c < columns; c++)
                    {
                        var v = ReadColumn(stmt, c);
                        if (c == 0) first = v;
                        values[names[c]] = v;
                    }
                    rows.Add(new SqliteRow(values, first));
                }
                if (rc != SqliteNative.DONE) throw new SqliteException(rc, Error());
                return rows;
            }
            finally
            {
                SqliteNative.sqlite3_finalize(stmt);
            }
        }

        public object Scalar(string sql, params (string name, object value)[] args)
        {
            var rows = Query(sql, args);
            return rows.Count == 0 ? null : rows[0].First;
        }

        public long LastInsertRowId => SqliteNative.sqlite3_last_insert_rowid(_db);

        /// <summary>Runs <paramref name="work"/> in a transaction; rolls back on exceptions.</summary>
        public void InTransaction(Action work)
        {
            Execute("BEGIN IMMEDIATE;");
            try
            {
                work();
                Execute("COMMIT;");
            }
            catch
            {
                Execute("ROLLBACK;");
                throw;
            }
        }

        public void Dispose()
        {
            if (_db == IntPtr.Zero) return;
            SqliteNative.sqlite3_close_v2(_db);
            _db = IntPtr.Zero;
        }

        private IntPtr Prepare(string sql, (string name, object value)[] args)
        {
            if (_db == IntPtr.Zero) throw new ObjectDisposedException(nameof(SqliteConnection));
            var bytes = SqliteNative.Utf8(sql);
            int rc = SqliteNative.sqlite3_prepare_v2(_db, bytes, bytes.Length, out var stmt, IntPtr.Zero);
            if (rc != SqliteNative.OK) throw new SqliteException(rc, $"{Error()} in: {sql}");
            if (args == null) return stmt;
            foreach (var (name, value) in args)
            {
                int index = SqliteNative.sqlite3_bind_parameter_index(stmt, SqliteNative.Utf8(name));
                if (index == 0)
                {
                    SqliteNative.sqlite3_finalize(stmt);
                    throw new ArgumentException($"Parameter {name} not found in: {sql}");
                }
                Bind(stmt, index, value);
            }
            return stmt;
        }

        private static void Bind(IntPtr stmt, int index, object value)
        {
            switch (value)
            {
                case null: SqliteNative.sqlite3_bind_null(stmt, index); break;
                case bool b: SqliteNative.sqlite3_bind_int64(stmt, index, b ? 1 : 0); break;
                case int i: SqliteNative.sqlite3_bind_int64(stmt, index, i); break;
                case long l: SqliteNative.sqlite3_bind_int64(stmt, index, l); break;
                case float f: SqliteNative.sqlite3_bind_double(stmt, index, f); break;
                case double d: SqliteNative.sqlite3_bind_double(stmt, index, d); break;
                case DateTime dt: BindText(stmt, index, dt.ToUniversalTime().ToString("o")); break;
                default: BindText(stmt, index, value.ToString()); break;
            }
        }

        private static void BindText(IntPtr stmt, int index, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            SqliteNative.sqlite3_bind_text(stmt, index, bytes, bytes.Length, SqliteNative.TRANSIENT);
        }

        private static object ReadColumn(IntPtr stmt, int c)
        {
            switch (SqliteNative.sqlite3_column_type(stmt, c))
            {
                case SqliteNative.INTEGER: return SqliteNative.sqlite3_column_int64(stmt, c);
                case SqliteNative.FLOAT: return SqliteNative.sqlite3_column_double(stmt, c);
                case SqliteNative.NULL: return null;
                default:
                    int n = SqliteNative.sqlite3_column_bytes(stmt, c);
                    return SqliteNative.PtrToUtf8(SqliteNative.sqlite3_column_text(stmt, c), n);
            }
        }

        private string Error() => SqliteNative.PtrToUtf8(SqliteNative.sqlite3_errmsg(_db));
    }
}
