using NGeoNames.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NGeoNames
{
	public class ReverseGeoCode<T>
		where T : IGeoLocation, new()
	{
		private KdTree.KdTree<double, T> _tree;

		public ReverseGeoCode()
			: this(Enumerable.Empty<T>()) { }

		public ReverseGeoCode(IEnumerable<T> nodes)
		{
			_tree = new KdTree.KdTree<double, T>(3, new DoubleMath());
			AddRange(nodes);
			Balance();
		}

		public void Add(T node)
		{
			_tree.Add(GeoUtil.GetCoord(node), node);
		}

		public void AddRange(IEnumerable<T> nodes)
		{
			foreach (var i in nodes)
				Add(i);
		}

		public Task AddAsync(T node)
		{
			return Task.Run(() => Add(node));
		}

		public Task AddRangeAsync(IEnumerable<T> nodes)
		{
			return Task.Run(() => AddRange(nodes));
		}

		public void Balance()
		{
			if (_tree.Count > 0)
				_tree.Balance();
		}

		public Task BalanceAsync()
		{
			return Task.Run(() => Balance());
		}

		public IEnumerable<T> RadialSearch(double lat, double lng, int maxcount)
		{
			return RadialSearch(CreateFromLatLong(lat, lng), maxcount);
		}

		public IEnumerable<T> RadialSearch(T center, int maxcount)
		{
			return RadialSearch(center, double.MaxValue, maxcount);
		}

		public IEnumerable<T> RadialSearch(double lat, double lng, double radius)
		{
			return RadialSearch(CreateFromLatLong(lat, lng), radius);
		}

		public IEnumerable<T> RadialSearch(T center, double radius)
		{
			return RadialSearch(center, radius, _tree.Count);
		}

		public IEnumerable<T> RadialSearch(double lat, double lng, double radius, int maxcount)
		{
			return RadialSearch(CreateFromLatLong(lat, lng), radius, maxcount);
		}

		public IEnumerable<T> RadialSearch(T center, double radius, int maxcount)
		{
			return _tree.RadialSearch(GeoUtil.GetCoord(center), radius, maxcount).Select(v => v.Value);
		}

		public Task<IEnumerable<T>> RadialSearchAsync(double lat, double lng, double radius, int maxcount)
		{
			return Task.Run(() => RadialSearch(lat, lng, radius, maxcount));
		}

		public Task<IEnumerable<T>> RadialSearchAsync(T center, double radius, int maxcount)
		{
			return Task.Run(() => RadialSearch(center, radius, maxcount));
		}

		public IEnumerable<T> NearestNeighbourSearch(double lat, double lng)
		{
			return NearestNeighbourSearch(CreateFromLatLong(lat, lng), _tree.Count);
		}

		public IEnumerable<T> NearestNeighbourSearch(double lat, double lng, int maxcount)
		{
			return NearestNeighbourSearch(CreateFromLatLong(lat, lng), maxcount);
		}

		public IEnumerable<T> NearestNeighbourSearch(T center)
		{
			return NearestNeighbourSearch(center, _tree.Count);
		}

		public IEnumerable<T> NearestNeighbourSearch(T center, int maxcount)
		{
			return _tree.GetNearestNeighbours(GeoUtil.GetCoord(center), maxcount).Select(v => v.Value);
		}

		public Task<IEnumerable<T>> NearestNeighbourSearchAsync(double lat, double lng, int maxcount)
		{
			return Task.Run(() => NearestNeighbourSearch(lat, lng, maxcount));
		}

		public Task<IEnumerable<T>> NearestNeighbourSearchAsync(T center, int maxcount)
		{
			return Task.Run(() => NearestNeighbourSearch(center, maxcount));
		}

		public async Task<IEnumerable<T>> RadialSearchAsync(double lat, double lng, int maxcount)
		{
			return await Task.FromResult(RadialSearch(lat, lng, maxcount));
		}

		public async Task<IEnumerable<T>> RadialSearchAsync(T center, double radius)
		{
			return await Task.FromResult(RadialSearch(center, radius));
		}

		public T CreateFromLatLong(double lat, double lng)
		{
			return new T()
			{
				Latitude = lat,
				Longitude = lng
			};
		}
	}

	internal class DoubleMath : KdTree.Math.TypeMath<double>
	{
		public override double Add(double a, double b) => a + b;

		public override bool AreEqual(double a, double b) => a == b;

		public override int Compare(double a, double b) => a.CompareTo(b);

		public override double MaxValue => double.MaxValue;
		public override double MinValue => double.MinValue;

		public override double Multiply(double a, double b) => a * b;

		public override double NegativeInfinity => double.NegativeInfinity;
		public override double PositiveInfinity => double.PositiveInfinity;

		public override double Subtract(double a, double b) => a - b;

		public override double Zero => 0;

		public override double DistanceSquaredBetweenPoints(double[] a, double[] b)
		{
			double distance = Zero;
			int dimensions = a.Length;

			for (var dimension = 0; dimension < dimensions; dimension++)
			{
				double distOnThisAxis = Subtract(a[dimension], b[dimension]);
				double distOnThisAxisSquared = Multiply(distOnThisAxis, distOnThisAxis);

				distance = Add(distance, distOnThisAxisSquared);
			}

			return distance;
		}
	}
}