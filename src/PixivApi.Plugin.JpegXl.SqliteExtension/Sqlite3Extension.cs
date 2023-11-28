#pragma warning disable IDE1006
public struct sqlite3_context
{
}

public struct sqlite3_stmt
{
}

public struct sqlite3_value
{
}

public struct sqlite3_blob
{
}

public struct sqlite3_mutex
{
}

public struct sqlite3_module
{
}

public struct sqlite3_backup
{
}

public struct sqlite3_vfs
{
}

public struct sqlite3_callback
{
}

public struct sqlite3
{
}

public unsafe struct sqlite3_api_routines
{
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void*> aggregate_context;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int> aggregate_count;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*, int, delegate* unmanaged[Cdecl]<void*, void>, int> bind_blob;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, double, int> bind_double;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int, int> bind_int;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, long, int> bind_int64;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> bind_null;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> bind_parameter_count;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, byte*, int> bind_parameter_index;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> bind_parameter_name;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*, int, delegate* unmanaged[Cdecl]<void*, void>, int> bind_text;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*, int, delegate* unmanaged[Cdecl]<void*, void>, int> bind_text16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, sqlite3_value*, int> bind_value;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, int, int>, void*, int> busy_handler;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, int> busy_timeout;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> changes;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> close;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, delegate* unmanaged[Cdecl]<void*, sqlite3*, int, byte*, void>, int> collation_needed;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, delegate* unmanaged[Cdecl]<void*, sqlite3*, int, void*, void>, int> collation_needed16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_blob;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_bytes;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_bytes16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> column_count;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_database_name;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_database_name16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_decltype;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_decltype16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, double> column_double;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_int;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, long> column_int64;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_name;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_name16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_origin_name;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_origin_name16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_table_name;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_table_name16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_text;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_text16;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_type;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, sqlite3_value*> column_value;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, int>, void*, void*> commit_hook;
  public delegate* unmanaged[Cdecl]<byte*, int> complete;
  public delegate* unmanaged[Cdecl]<void*, int> complete16;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, void*, delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int>, int> create_collation;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, int, void*, delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int>, int> create_collation16;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, int, void*, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, void>, int> create_function;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, int, int, void*, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, void>, int> create_function16;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, sqlite3_module*, void*, int> create_module;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> data_count;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, sqlite3*> db_handle;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int> declare_vtab;
  public delegate* unmanaged[Cdecl]<int, int> enable_shared_cache;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> errcode;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*> errmsg;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*> errmsg16;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, sqlite3_callback, void*, byte**, int> exec;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> expired;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> finalize;
  public delegate* unmanaged[Cdecl]<void*, void> free;
  public delegate* unmanaged[Cdecl]<byte**, void> free_table;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> get_autocommit;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void*> get_auxdata;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, byte***, int*, int*, byte**, int> get_table;
  public delegate* unmanaged[Cdecl]<int> global_recover;
  public delegate* unmanaged[Cdecl]<sqlite3*, void> interruptx;
  public delegate* unmanaged[Cdecl]<sqlite3*, long> last_insert_rowid;
  public delegate* unmanaged[Cdecl]<byte*> libversion;
  public delegate* unmanaged[Cdecl]<int> libversion_number;
  public delegate* unmanaged[Cdecl]<int, void*> malloc;
  public void* mprintf;
  public delegate* unmanaged[Cdecl]<byte*, sqlite3**, int> open;
  public delegate* unmanaged[Cdecl]<void*, sqlite3**, int> open16;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, sqlite3_stmt**, byte**, int> prepare;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, int, sqlite3_stmt**, void**, int> prepare16;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, byte*, ulong, void>, void*, void*> profile;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, delegate* unmanaged[Cdecl]<void*, int>, void*, void> progress_handler;
  public delegate* unmanaged[Cdecl]<void*, int, void*> realloc;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> reset;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*, int, delegate* unmanaged[Cdecl]<void*, void>, void> result_blob;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, double, void> result_double;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, byte*, int, void> result_error;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*, int, void> result_error16;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void> result_int;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, long, void> result_int64;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void> result_null;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, byte*, int, delegate* unmanaged[Cdecl]<void*, void>, void> result_text;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*, int, delegate* unmanaged[Cdecl]<void*, void>, void> result_text16;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*, int, delegate* unmanaged[Cdecl]<void*, void>, void> result_text16be;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*, int, delegate* unmanaged[Cdecl]<void*, void>, void> result_text16le;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, sqlite3_value*, void> result_value;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, void>, void*, void*> rollback_hook;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, int, byte*, byte*, byte*, byte*, int>, void*, int> set_authorizer;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void*, delegate* unmanaged[Cdecl]<void*, void>, void> set_auxdata;
  public void* snprintf;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> step;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, byte*, byte*, byte**, byte**, int*, int*, int*, int> table_column_metadata;
  public delegate* unmanaged[Cdecl]<void> thread_cleanup;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> total_changes;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, byte*, void>, void*, void*> trace;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, sqlite3_stmt*, int> transfer_bindings;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, int, byte*, byte*, long, void>, void*, void*> update_hook;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void*> user_data;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, void*> value_blob;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, int> value_bytes;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, int> value_bytes16;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, double> value_double;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, int> value_int;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, long> value_int64;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, int> value_numeric_type;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, byte*> value_text;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, void*> value_text16;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, void*> value_text16be;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, void*> value_text16le;
  public delegate* unmanaged[Cdecl]<sqlite3_value*, int> value_type;
  public void* vmprintf;
  /* Added ??? */
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, int> overload_function;
  /* Added by 3.3.13 */
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, sqlite3_stmt**, byte**, int> prepare_v2;
  public delegate* unmanaged[Cdecl]<sqlite3*, void*, int, sqlite3_stmt**, void**, int> prepare16_v2;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> clear_bindings;
  /* Added by 3.4.1 */
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, sqlite3_module*, void*, delegate* unmanaged[Cdecl]<void*, void>, int> create_module_v2;
  /* Added by 3.5.0 */
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int, int> bind_zeroblob;
  public delegate* unmanaged[Cdecl]<sqlite3_blob*, int> blob_bytes;
  public delegate* unmanaged[Cdecl]<sqlite3_blob*, int> blob_close;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, byte*, byte*, long, int, sqlite3_blob**, int> blob_open;
  public delegate* unmanaged[Cdecl]<sqlite3_blob*, void*, int, int, int> blob_read;
  public delegate* unmanaged[Cdecl]<sqlite3_blob*, void*, int, int, int> blob_write;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, void*, delegate* unmanaged[Cdecl]<void*, int, void*, int, void*, int>, delegate* unmanaged[Cdecl]<void*, void>, int> create_collation_v2;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, void*, int> file_control;
  public delegate* unmanaged[Cdecl]<int, long> memory_highwater;
  public delegate* unmanaged[Cdecl]<long> memory_used;
  public delegate* unmanaged[Cdecl]<int, sqlite3_mutex*> mutex_alloc;
  public delegate* unmanaged[Cdecl]<sqlite3_mutex*, void> mutex_enter;
  public delegate* unmanaged[Cdecl]<sqlite3_mutex*, void> mutex_free;
  public delegate* unmanaged[Cdecl]<sqlite3_mutex*, void> mutex_leave;
  public delegate* unmanaged[Cdecl]<sqlite3_mutex*, int> mutex_try;
  public delegate* unmanaged[Cdecl]<byte*, sqlite3**, int, byte*, int> open_v2;
  public delegate* unmanaged[Cdecl]<int, int> release_memory;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void> result_error_nomem;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, void> result_error_toobig;
  public delegate* unmanaged[Cdecl]<int, int> sleep;
  public delegate* unmanaged[Cdecl]<int, void> soft_heap_limit;
  public delegate* unmanaged[Cdecl]<byte*, sqlite3_vfs*> vfs_find;
  public delegate* unmanaged[Cdecl]<sqlite3_vfs*, int, int> vfs_register;
  public delegate* unmanaged[Cdecl]<sqlite3_vfs*, int> vfs_unregister;
  public delegate* unmanaged[Cdecl]<int> xthreadsafe;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void> result_zeroblob;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, int, void> result_error_code;
  public void* test_control;
  public delegate* unmanaged[Cdecl]<int, void*, void> randomness;
  public delegate* unmanaged[Cdecl]<sqlite3_context*, sqlite3*> context_db_handle;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, int> extended_result_codes;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, int, int> limit;
  public delegate* unmanaged[Cdecl]<sqlite3*, sqlite3_stmt*, sqlite3_stmt*> next_stmt;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, byte*> sql;
  public delegate* unmanaged[Cdecl]<int, int*, int*, int, int> status;
  public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_finish;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, sqlite3*, byte*, sqlite3_backup*> backup_init;
  public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_pagecount;
  public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_remaining;
  public delegate* unmanaged[Cdecl]<sqlite3_backup*, int, int> backup_step;
  public delegate* unmanaged[Cdecl]<int, byte*> compileoption_get;
  public delegate* unmanaged[Cdecl]<byte*, int> compileoption_used;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, int, void*, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, int, sqlite3_value**, void>, delegate* unmanaged[Cdecl]<sqlite3_context*, void>, delegate* unmanaged[Cdecl]<void*, void>, int> create_function_v2;
  public void* db_config;
  public delegate* unmanaged[Cdecl]<sqlite3*, sqlite3_mutex*> db_mutex;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, int*, int*, int, int> db_status;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> extended_errcode;
  public void* log;
  public delegate* unmanaged[Cdecl]<long, long> soft_heap_limit64;
  public delegate* unmanaged[Cdecl]<byte*> sourceid;
  public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int, int> stmt_status;
  public delegate* unmanaged[Cdecl]<byte*, byte*, int, int> strnicmp;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void**, int, void>, void*, int> unlock_notify;
  public delegate* unmanaged[Cdecl]<sqlite3*, int, int> wal_autocheckpoint;
  public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int> wal_checkpoint;
  public delegate* unmanaged[Cdecl]<sqlite3*, delegate* unmanaged[Cdecl]<void*, sqlite3*, byte*, int, int>, void*, void*> wal_hook;
  public delegate* unmanaged[Cdecl]<sqlite3_blob*, long, int> blob_reopen;
  public void* vtab_config;
  public delegate* unmanaged[Cdecl]<sqlite3*, int> vtab_on_conflict;
};
#pragma warning restore IDE1006
