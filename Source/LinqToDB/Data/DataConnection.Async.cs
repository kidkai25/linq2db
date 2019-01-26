﻿using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using RetryPolicy;

	public partial class DataConnection
	{
		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level. If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			await EnsureConnectionAsync(cancellationToken);

			return BeginTransaction();
		}

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level. If connection already have transaction, it will be rolled back.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			await EnsureConnectionAsync(cancellationToken);

			return BeginTransaction(isolationLevel);
		}

		/// <summary>
		/// Ensure that database connection opened. If opened connection missing, it will be opened asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		public async Task EnsureConnectionAsync(CancellationToken cancellationToken = default)
		{
			if (_connection == null)
			{
				if (_connectionFactory != null)
					_connection = _connectionFactory();
				else
					_connection = DataProvider.CreateConnection(ConnectionString);

				if (RetryPolicy != null)
					_connection = new RetryingDbConnection(this, (DbConnection)_connection, RetryPolicy);
			}

			if (_connection.State == ConnectionState.Closed)
			{
				try
				{
					if (_connection is RetryingDbConnection retrying)
						await retrying.OpenAsync(cancellationToken);
					else
						await ((DbConnection)_connection).OpenAsync(cancellationToken);

					_closeConnection = true;

					var task = OnConnectionOpenedAsync?.Invoke(this, _connection, cancellationToken);
					if (task != null)
						await task;
				}
				catch (Exception ex)
				{
					if (TraceSwitch.TraceError)
					{
						OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
						{
							TraceLevel     = TraceLevel.Error,
							DataConnection = this,
							StartTime      = DateTime.UtcNow,
							Exception      = ex,
							IsAsync        = true,
						});
					}

					throw;
				}
			}
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					StartTime      = now,
					DataConnection = this,
					Command        = Command,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = ret,
						IsAsync         = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}

		internal async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteScalarAsync(cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					StartTime      = now,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteScalarAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						IsAsync         = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}

		internal async Task<DbDataReader> ExecuteReaderAsync(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteReaderAsync(commandBehavior, cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					StartTime      = now,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteReaderAsync(commandBehavior, cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel     = TraceLevel.Info,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						IsAsync        = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}
	}
}
