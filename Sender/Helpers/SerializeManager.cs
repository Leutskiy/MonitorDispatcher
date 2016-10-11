using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Helpers
{
    public static class SerializeManager
    {
        public static void SerializeListData<T>(T settings, string serializableDataFile) where T : class
        {
            if (!File.Exists(serializableDataFile))
                return;

            using (Stream fileStream = new FileStream(serializableDataFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var binFormatter = new BinaryFormatter();
                binFormatter.Serialize(fileStream, settings);
            }
        }
    }
}
