namespace System.Threading.Tasks
{
	/// <summary>
	/// Represents a keyed task item
	/// </summary>
	public interface IKeyedItem : IDisposable
	{
		/// <summary>
		/// Attempts to set the result of the task item
		/// </summary>
		/// <returns>True if the result was set, False if a value already exists or or the item was disposed</returns>
		bool TrySetResult();

		/// <summary>
		/// Attempts to cancel the task item
		/// </summary>
		/// <returns>True if the task was cancelled, False if a value already exists or or the item was disposed</returns>
		bool TrySetCanceled();

		/// <summary>
		/// Attempts to fault the task item
		/// </summary>
		/// <param name="exception">The exception to record as the fault</param>
		/// <returns>True if the task was faulted, False if a value already exists or or the item was disposed</returns>
		bool TrySetException(Exception exception);

		//****************************************

		/// <summary>
		/// Gets the Task that is controlled by this item
		/// </summary>
		Task Task { get; }
	}

	/// <summary>
	/// Represents a keyed task item
	/// </summary>
	/// <typeparam name="TResult">The type of result to return</typeparam>
	public interface IKeyedItem<TResult> : IDisposable
	{
		/// <summary>
		/// Attempts to set the result of the task item
		/// </summary>
		/// <param name="result">The result of the item</param>
		/// <returns>True if the result was set, False if a value already exists or or the item was disposed</returns>
		bool TrySetResult(TResult result);

		/// <summary>
		/// Attempts to cancel the task item
		/// </summary>
		/// <returns>True if the task was cancelled, False if a value already exists or or the item was disposed</returns>
		bool TrySetCanceled();

		/// <summary>
		/// Attempts to fault the task item
		/// </summary>
		/// <param name="exception">The exception to record as the fault</param>
		/// <returns>True if the task was faulted, False if a value already exists or or the item was disposed</returns>
		bool TrySetException(Exception exception);

		//****************************************

		/// <summary>
		/// Gets the Task that is controlled by this item
		/// </summary>
		Task<TResult> Task { get; }
	}
}
