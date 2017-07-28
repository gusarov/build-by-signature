using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("abc")]

namespace Sample
{
	public class PublicClass
	{
	}

	class InternalClass
	{
	}

	public class Outer
	{
		// methods
		public void qPublic() { }
		internal void qInternal() { }
		private void qPrivate() { }
		protected void qProtected() { }
		protected internal void qProtecedInternal() { }

		// misc
		static Outer() { }
		public Outer() {}
		public Outer(bool q) {}
		public event Action EventPublic;
		public int Property { get; set; }
		public int this[int x, string y]
		{
			get { return 0; }
		}

		// fields
		public int FieldPublic;
		internal int FieldInternal;
		protected int FieldProtected;
		protected internal int FieldProtectedInternal;
		int FieldPrivate;

		// nested
		public class NestedPublic
		{

		} 

		internal class NestedInternal
		{
			public class EeeeHaaa
			{

			}
		}

		private class NestedPrivate
		{
			public class YaaHoo
			{

			}
		}

		sealed protected class NestedProtected
		{
			public void x()
			{
				
			}
		}

		protected internal class NestedProtectedInternal
		{

		}

	}
}
