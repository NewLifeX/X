namespace System.Threading;

public struct CancellationTokenRegistration : IDisposable, IEquatable<CancellationTokenRegistration>
{
	private int id;

	private CancellationTokenSource source;

	internal CancellationTokenRegistration(int id, CancellationTokenSource source)
	{
		this.id = id;
		this.source = source;
	}

	public void Dispose()
	{
		source?.RemoveCallback(this);
	}

	public bool Equals(CancellationTokenRegistration other)
	{
		if (id == other.id)
		{
			return source == other.source;
		}
		return false;
	}

	public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right)
	{
		return !left.Equals(right);
	}

	public override int GetHashCode()
	{
		return id.GetHashCode() ^ source.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is CancellationTokenRegistration))
		{
			return false;
		}
		return Equals((CancellationTokenRegistration)obj);
	}
}
