using EPiServer.Framework.TypeScanner;
using System;
using System.Collections.Generic;

namespace Bokrondellen.Initialization
{
	class NullTypeScanner : ITypeScannerLookup
	{
		public IEnumerable<Type> AllTypes
		{
			get { return new Type[0]; }
		}
	}
}
