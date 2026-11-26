using System;

namespace RsAgent
{
    internal static class StorageManufacturerTests
    {
        private static void Check(string input, string expected)
        {
            var actual = InventoryCollector.ReliableDiskManufacturer(input);
            if (actual != expected)
                throw new Exception("Unexpected disk manufacturer: " + actual);
        }

        public static int Main()
        {
            Check("(Unidades de disco estándar)", "");
            Check("(Standard disk drives)", "");
            Check(" Unknown ", "");
            Check("", "");
            Check(" Micron ", "Micron");
            Check("Samsung Electronics", "Samsung Electronics");
            Console.WriteLine("Storage manufacturer checks passed");
            return 0;
        }
    }
}
