using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kookerella.FileScanner
{
	public class Changes<T>
	{
		public IEnumerable<T> Added { get; private set; }
		public IEnumerable<T> Deleted { get; private set; }

		public Changes(IEnumerable<T> added, IEnumerable<T> deleted)
		{
			this.Added = added;
			this.Deleted = deleted;
		}
	}

	public static class Enumerable
	{
		private static IEnumerable<Tuple<T, T>> InTwos<T>(this IEnumerable<T> ts)
		{
			var e = ts.GetEnumerator();
			if (e.MoveNext())
			{
				var last = e.Current;
				while (e.MoveNext())
				{
					yield return Tuple.Create(last, e.Current);
					last = e.Current;
				}
			}
		}

		public static Changes<T> DiffList<T, Key>(T[] ts1, T[] ts2, Func<T, Key> toKey)
			where Key : struct
		{
			var lookup1 = ts1.ToLookup(toKey, i => i);
			var lookup2 = ts2.ToLookup(toKey, i => i);

			var t1sAndMatchingT2s =
				(from t1 in ts1
				 let matching = lookup2[toKey(t1)]
				 select new { element = t1, matching = matching.ToArray() }).ToArray();

			var deleted =
				from e in t1sAndMatchingT2s
				where !e.matching.Any()
				select e.element;

			var t2sAndMatchingT1s =
				(from t2 in ts2
				 let matching = lookup1[toKey(t2)]
				 select new { element = t2, matching = matching.ToArray() }).ToArray();

			var added =
				from e in t2sAndMatchingT1s
				where !e.matching.Any()
				select e.element;

			return new Changes<T>(added, deleted);
		}

		public static IEnumerable<Changes<T>> Differential<T, Key>(this IEnumerable<IEnumerable<T>> tts,
			Func<T, Key> toKey)
			where Key : struct
		{
			return from pair in
						(from ts in tts
						 select ts.ToArray()).InTwos()
				   select DiffList(pair.Item1, pair.Item2, toKey);
		}
	}

	public struct Key
	{
#pragma warning disable 414
		// not accessed directly but through "==" 
		public string FullName;
		public long Length;
		public DateTime LastWriteTimeUtc;
#pragma warning restore 414
	}

	public interface IScanner
	{
		bool IsDone { set; get; }
		T Load<T>(string uri, Func<Stream, T> f);
	}

	public class SimpleFolderScanner : IScanner
	{
		//        struct Key
		//        {
		//#pragma warning disable 414
		//            // not accessed directly but through "==" 
		//            public string FullName;
		//            public long Length;
		//            public DateTime LastWriteTimeUtc;
		//#pragma warning restore 414
		//        }

		static Tuple<SimpleFolderScanner, IEnumerable<FileInfo>> NewSimpleFolderScanner(string folderName, string filter)
		{
			var scanner = new SimpleFolderScanner(folderName, filter);

			return Tuple.Create(scanner, scanner.EnumerateAdds());
		}

		public static SimpleFolderScanner NewSimpleFolderScanner(string folderName, string filter, Action<FileInfo> addAction, Action<Exception> @catch)
		{
			var scanner = NewSimpleFolderScanner(folderName, filter);

			Task.Run(() =>
			{
				foreach (var file in scanner.Item2)
				{
					try
					{
						addAction(file);
					}
					catch (Exception e)
					{
						@catch(e);
					}
				}
			});

			return scanner.Item1;
		}

		static IEnumerable<FileInfo[]> EnumerateFilesForever(string folder, string filter)
		{
			var fi = new DirectoryInfo(folder);

			while (true)
			{
				var answer = fi.EnumerateFiles(filter).ToArray();
				yield return answer;
				System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(10 * 1000));
			}
		}

		static Key ToKey(FileInfo fi)
		{
			return new Key()
			{
				FullName = fi.FullName,
				Length = fi.Length,
				LastWriteTimeUtc = fi.LastWriteTimeUtc
			};

		}

		public bool IsDone { set; get; }
		readonly string folderName;
		readonly string filter;

		SimpleFolderScanner(string folderName, string filter)
		{
			this.folderName = folderName;
			this.filter = filter;
		}

		IEnumerable<FileInfo> EnumerateAdds()
		{
			// base state is an empty folder...then we start looking in the folder
			var enumerateFiles =
				(new FileInfo[][] { new FileInfo[] { } }.
				Concat(EnumerateFilesForever(folderName, filter))).
				Differential(ToKey);

			foreach (var change in
						(from change in enumerateFiles
						 select change.Added))
			{
				if (this.IsDone)
				{
					break;
				}
				else
				{
					foreach (var fileInfo in change)
					{
						yield return fileInfo;
					}
				}
			}
		}

		public T Load<T>(string uri, Func<Stream, T> f)
		{
			using (var s = System.IO.File.Open(uri, FileMode.Open))
			{
				return f(s);
			}
		}
	}
}
