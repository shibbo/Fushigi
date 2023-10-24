using System.Runtime.CompilerServices;

namespace Fushigi.Byml
{
    public class BymlWriter
    {
        private readonly Writer.BymlStringTable HashKeyStringTable = new();
        private readonly Writer.BymlStringTable StringTable = new();
        private readonly List<Writer.BymlContainer> ContainerList = new();
        private readonly Writer.BymlBigDataList BigDataList = new();
        private int CurrentContainerIndex = -1;

        /* MK8DX oddity. */
        public Writer.BymlPathArray? PathArray = null;

        private Writer.BymlContainer GetCurrentContainer() => ContainerList[CurrentContainerIndex];

        public void AddBool(bool value) => GetCurrentContainer().AddBool(value);

        public void AddInt(int value) => GetCurrentContainer().AddInt(value);

        public void AddInt(int[] array)
        {
            PushArray();
            foreach (var v in array)
            {
                GetCurrentContainer().AddInt(v);
            }
            Pop();
        }

        public void PushArray()
        {
            var newArray = new Writer.BymlArray(StringTable);
            if (CurrentContainerIndex >= 0)
            {
                GetCurrentContainer().AddArray(newArray);
            }
            PushContainer(newArray);
        }

        public void Pop()
        {
            CurrentContainerIndex--;
        }

        public void AddUInt(uint value) => GetCurrentContainer().AddUInt(value);

        public void AddUint(uint[] array)
        {
            PushArray();
            foreach (var v in array)
            {
                GetCurrentContainer().AddUInt(v);
            }
            Pop();
        }
        public void AddFloat(float value) => GetCurrentContainer().AddFloat(value);

        public void AddFloat(float[] array)
        {
            PushArray();
            foreach (var v in array)
            {
                GetCurrentContainer().AddFloat(v);
            }
            Pop();
        }

        public void AddInt64(long value) => GetCurrentContainer().AddInt64(value, BigDataList);

        public void AddUInt64(ulong value) => GetCurrentContainer().AddUInt64(value, BigDataList);

        public void AddDouble(double value) => GetCurrentContainer().AddDouble(value, BigDataList);

        public void AddBinary(byte[] value) => GetCurrentContainer().AddBinary(value, BigDataList);

        public void AddString(string value) => GetCurrentContainer().AddString(value);

        public void AddNull() => GetCurrentContainer().AddNull();

        public void AddBool(string key, bool value) => GetCurrentContainer().AddBool(key, value);

        public void AddInt(string key, int value) => GetCurrentContainer().AddInt(key, value);

        public void AddInt(string key, int[] array)
        {
            PushArray(key);
            foreach (var v in array)
            {
                GetCurrentContainer().AddInt(v);
            }
            Pop();
        }

        public void PushArray(string key)
        {
            var newArray = new Writer.BymlArray(StringTable);
            GetCurrentContainer().AddArray(key, newArray);
            PushContainer(newArray);
        }

        public void AddUInt(string key, uint value) => GetCurrentContainer().AddUInt(key, value);

        public void AddUInt(string key, uint[] array)
        {
            PushArray(key);
            foreach (var v in array)
            {
                GetCurrentContainer().AddUInt(v);
            }
            Pop();
        }

        public void AddFloat(string key, float value) => GetCurrentContainer().AddFloat(key, value);

        public void AddFloat(string key, float[] array)
        {
            PushArray(key);
            foreach (var v in array)
            {
                GetCurrentContainer().AddFloat(v);
            }
            Pop();
        }

        public void AddInt64(string key, long value) => GetCurrentContainer().AddInt64(key, value, BigDataList);

        public void AddUInt64(string key, ulong value) => GetCurrentContainer().AddUInt64(key, value, BigDataList);

        public void AddDouble(string key, double value) => GetCurrentContainer().AddDouble(key, value, BigDataList);

        public void AddBinary(string key, byte[] value) => GetCurrentContainer().AddBinary(key, value, BigDataList);

        public void AddString(string key, string value) => GetCurrentContainer().AddString(key, value);

        public void AddNull(string key) => GetCurrentContainer().AddNull(key);

        public void PushHash()
        {
            var newHash = new Writer.BymlHash(HashKeyStringTable, StringTable);
            if (CurrentContainerIndex >= 0)
            {
                GetCurrentContainer().AddHash(newHash);
            }
            PushContainer(newHash);
        }

        public void PushContainer(Writer.BymlContainer container)
        {
            CurrentContainerIndex++;
            ContainerList.Insert(CurrentContainerIndex, container);
        }

        public void PushHash(string key)
        {
            var newHash = new Writer.BymlHash(HashKeyStringTable, StringTable);
            GetCurrentContainer().AddHash(key, newHash);
            PushContainer(newHash);
        }

        public void PushIter(IBymlNode node)
        {
            PushLocalIter(node, null);
        }

        public void PushLocalIter(IBymlNode rootNode, string? key)
        {
            var rootAsHash = rootNode as BymlHashTable;
            var rootAsArray = rootNode as BymlArrayNode;
            int size;

            if (rootAsHash != null)
            {
                size = rootAsHash.Pairs.Length;
                if (key != null)
                {
                    PushHash(key);
                }
                else
                {
                    PushHash();
                }
            }
            else
            {
                if (rootAsArray == null)
                    return;

                size = rootAsArray.Length;
                if (key != null)
                    PushArray(key);
                else
                    PushArray();
            }

            if (size > 0)
            {

                for (int i = 0; i < size; i++)
                {
                    IBymlNode node;
                    string? nodeName = null;

                    if (rootAsHash != null)
                    {
                        var pair = rootAsHash.Pairs[i];
                        node = pair.Value;
                        nodeName = pair.Name;
                    }
                    else
                    {
                        node = rootAsArray[i];
                    }

                    if (node is BymlArrayNode || node is BymlHashTable)
                    {
                        PushLocalIter(node, nodeName);
                        continue;
                    }

                    void PushValue()
                    {
                        dynamic value = ((dynamic)node).Data;
                        switch (node.Id)
                        {
                            case BymlNodeId.Bin:
                                if (nodeName != null)
                                    AddBinary(nodeName, value);
                                else
                                    AddBinary(value);
                                break;
                            case BymlNodeId.Bool:
                                if (nodeName != null)
                                    AddBool(nodeName, value);
                                else
                                    AddBool(value);
                                break;
                            case BymlNodeId.Double:
                                if (nodeName != null)
                                    AddDouble(nodeName, value);
                                else
                                    AddDouble(value);
                                break;
                            case BymlNodeId.Float:
                                if (nodeName != null)
                                    AddFloat(nodeName, value);
                                else
                                    AddFloat(value);
                                break;
                            case BymlNodeId.Int:
                                if (nodeName != null)
                                    AddInt(nodeName, value);
                                else
                                    AddInt(value);
                                break;
                            case BymlNodeId.Int64:
                                if (nodeName != null)
                                    AddInt64(nodeName, value);
                                else
                                    AddInt64(value);
                                break;
                            case BymlNodeId.Null:
                                if (nodeName != null)
                                    AddNull(nodeName);
                                else
                                    AddNull();
                                break;
                            case BymlNodeId.String:
                                if (nodeName != null)
                                    AddString(nodeName, value);
                                else
                                    AddString(value);
                                break;
                            case BymlNodeId.UInt:
                                if (nodeName != null)
                                    AddUInt(nodeName, value);
                                else
                                    AddUInt(value);
                                break;
                            case BymlNodeId.UInt64:
                                if (nodeName != null)
                                    AddUInt64(nodeName, value);
                                else
                                    AddUInt64(value);
                                break;

                            default:
                                throw new Exception();
                        }
                    }
                    PushValue();
                }
            }
            Pop();
        }

        public void PushIter(string key, IBymlNode node)
        {
            PushLocalIter(node, key);
        }

        public uint CalcHeaderSize() => (uint)(Unsafe.SizeOf<BymlHeader>() + (PathArray != null ? Unsafe.SizeOf<uint>() : 0));

        public uint CalcPackSize()
        {
            uint size = CalcHeaderSize();
            size += (uint)HashKeyStringTable.CalcPackSize();
            size += (uint)StringTable.CalcPackSize();
            size += (uint)BigDataList.CalcPackSize();
            size += (uint)(PathArray?.CalcPackSize() ?? 0);
            foreach (var container in ContainerList)
            {
                size += (uint)container.CalcPackSize();
            }
            return size;
        }

        public void Write(Stream stream)
        {
            uint headerSize = CalcHeaderSize();
            uint hashKeyStringTableSize = (uint)HashKeyStringTable.CalcPackSize();
            uint stringTableSize = (uint)StringTable.CalcPackSize();
            uint bigDataListSize = (uint)BigDataList.CalcPackSize();

            uint hashKeyOffset = headerSize;
            uint stringTableOffset = headerSize + hashKeyStringTableSize;
            uint bigDataListOffset = stringTableOffset + stringTableSize;
            uint pathArrayOffset = bigDataListOffset + bigDataListSize;

            var rootOffset = (uint)BigDataList.SetOffset((int)bigDataListOffset);
            rootOffset = ContainerList.Count == 0 ? 0 : rootOffset;
            var rootOrPathArrayOffset = PathArray != null ? pathArrayOffset : rootOffset;

            BymlHeader header = new()
            {
                Magic = 0x4259,
                Version = 4,
                HashKeyOffset = HashKeyStringTable.IsEmpty() ? 0 : hashKeyOffset,
                StringTableOffset = StringTable.IsEmpty() ? 0 : stringTableOffset,
                RootOrPathArrayOffset = rootOrPathArrayOffset
            };

            if (PathArray != null)
            {
                stream.AsBinaryWriter().Write(rootOffset);
            }

            stream.Write(Utils.AsSpan(ref header));
            HashKeyStringTable.Write(stream);
            StringTable.Write(stream);
            BigDataList.Write(stream);
            PathArray?.Write(stream);

            uint offset = rootOffset;
            foreach (var container in ContainerList)
            {
                container.Offset = offset;
                offset += (uint)container.CalcPackSize();
            }
            foreach (var container in ContainerList)
            {
                container.WriteContainer(stream);
            }

        }
    }
}
