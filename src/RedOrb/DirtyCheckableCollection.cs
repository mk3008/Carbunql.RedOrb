using System.Collections;
using System.Collections.ObjectModel;

namespace RedOrb;

public interface IDirtyCheckableCollection
{
	IList RemovedCollection { get; }
}

public interface IDirtyCheckableCollection<T> : IDirtyCheckableCollection
{
	new IList<T> RemovedCollection { get; }
}

public class DirtyCheckableCollection<T> : ObservableCollection<T>, IDirtyCheckableCollection<T>
{
	private List<T> InnerRemovedCollection { get; } = new();

	public ReadOnlyCollection<T> RemovedCollection => InnerRemovedCollection.AsReadOnly();

	IList<T> IDirtyCheckableCollection<T>.RemovedCollection => InnerRemovedCollection;

	IList IDirtyCheckableCollection.RemovedCollection => InnerRemovedCollection;

	public void ResetCache()
	{
		InnerRemovedCollection.Clear();
	}

	protected override void ClearItems()
	{
		InnerRemovedCollection.AddRange(Items);
		base.ClearItems();
	}

	protected override void RemoveItem(int index)
	{
		var item = Items[index];
		InnerRemovedCollection.Add(item);
		base.RemoveItem(index);
	}
}
