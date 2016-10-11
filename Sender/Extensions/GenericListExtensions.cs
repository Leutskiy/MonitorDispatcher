using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sender.Extensions
{
    public static class GenericListExtensions
    {
        public static void SerializeListData<T>(this List<T> listData, string serializableDataFile)
        {
            if (!File.Exists(serializableDataFile))
                return;

            using (Stream fileStream = new FileStream(serializableDataFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var binFormatter = new BinaryFormatter();
                binFormatter.Serialize(fileStream, listData);
            }
        }
    }
}