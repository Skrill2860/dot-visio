using System;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GoldParser;

public enum EgtRecord : byte
{
    InitialStates = 73, // I
    Symbol = 83, // S
    Production = 82, // R   R for Rule (related productions)
    DfaState = 68, // D
    LrState = 76, // L
    Property = 112, // p
    CharRanges = 99, // c 
    Group = 103, // g
    TableCounts = 116 // t   Table Counts
}

public class EgtReader
{
    public enum EntryType : byte
    {
        Empty = 69, // E
        UInt16 = 73, // I - Unsigned, 2 byte
        String = 83, // S - Unicode format
        Boolean = 66, // B - 1 Byte, Value is 0 or 1
        Byte = 98, // b
        Error = 0
    }

    private const byte KRecordContentMulti = 77; // M
    private int _entriesRead;

    // Current record 
    private int _entryCount;
    private string _fileHeader;
    private BinaryReader? _reader;

    public EgtReader(BinaryReader reader)
    {
        _reader = reader;

        _entryCount = 0;
        _entriesRead = 0;
        _fileHeader = RawReadCString();
    }

    public bool RecordComplete()
    {
        return _entriesRead >= _entryCount;
    }

    public void Close()
    {
        if (_reader is null)
        {
            return;
        }

        _reader.Close();
        _reader = null;
    }

    public short EntryCount()
    {
        return (short)_entryCount;
    }

    public bool EndOfFile()
    {
        return _reader is null || _reader.BaseStream.Position == _reader.BaseStream.Length;
    }

    public string Header()
    {
        return _fileHeader;
    }

    public void Open(BinaryReader reader)
    {
        _reader = reader;

        _entryCount = 0;
        _entriesRead = 0;
        _fileHeader = RawReadCString();
    }

    public void Open(string path)
    {
        Open(new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)));
    }

    public Entry RetrieveEntry()
    {
        if (_reader is null)
        {
            throw new InvalidOperationException();
        }

        var result = new Entry();

        if (RecordComplete())
        {
            result.Type = EntryType.Empty;
            result.Value = "";
        }
        else
        {
            _entriesRead += 1;
            var type = _reader.ReadByte();
            result.Type = (EntryType)type;

            switch (type)
            {
                case (byte)EntryType.Empty:
                {
                    result.Value = "";
                    break;
                }

                case (byte)EntryType.Boolean:
                {
                    var b = _reader.ReadByte();
                    result.Value = b == 1;
                    break;
                }

                case (byte)EntryType.UInt16:
                {
                    result.Value = RawReadUInt16();
                    break;
                }

                case (byte)EntryType.String:
                {
                    result.Value = RawReadCString();
                    break;
                }

                case (byte)EntryType.Byte:
                {
                    result.Value = _reader.ReadByte();
                    break;
                }

                default:
                {
                    result.Type = EntryType.Error;
                    result.Value = "";
                    break;
                }
            }
        }

        return result;
    }

    private ushort RawReadUInt16()
    {
        // Read a uint in little endian. This is the format already used
        // by the .NET BinaryReader. However, it is good to specificially
        // define this given byte order can change depending on platform.

        int b0 = _reader!.ReadByte(); // Least significant byte first
        int b1 = _reader!.ReadByte();

        var result = (ushort)((b1 << 8) + b0);

        return result;
    }

    private string RawReadCString()
    {
        var text = "";
        var done = false;

        while (!done)
        {
            var char16 = RawReadUInt16();
            if (char16 == 0)
            {
                done = true;
            }
            else
            {
                text += Conversions.ToString(Strings.ChrW(char16));
            }
        }

        return text;
    }


    public string RetrieveString()
    {
        var e = RetrieveEntry();
        if (e.Type == EntryType.String)
        {
            return Conversions.ToString(e.Value)!;
        }

        throw new IoException(e.Type, _reader!);
    }

    public int RetrieveInt16()
    {
        var e = RetrieveEntry();
        if (e.Type == EntryType.UInt16)
        {
            return Conversions.ToInteger(e.Value);
        }

        throw new IoException(e.Type, _reader!);
    }

    public bool RetrieveBoolean()
    {
        var e = RetrieveEntry();
        if (e.Type == EntryType.Boolean)
        {
            return Conversions.ToBoolean(e.Value);
        }

        throw new IoException(e.Type, _reader!);
    }

    public byte RetrieveByte()
    {
        var e = RetrieveEntry();
        if (e.Type == EntryType.Byte)
        {
            return Conversions.ToByte(e.Value);
        }

        throw new IoException(e.Type, _reader!);
    }

    public bool GetNextRecord()
    {
        bool success;

        // ==== Finish current record
        while (_entriesRead < _entryCount)
        {
            RetrieveEntry();
        }

        // ==== Start next record
        var id = _reader!.ReadByte();

        if (id == KRecordContentMulti)
        {
            _entryCount = RawReadUInt16();
            _entriesRead = 0;
            success = true;
        }
        else
        {
            success = false;
        }

        return success;
    }

    ~EgtReader()
    {
        Close();
    }


    private class IoException : Exception
    {
        public IoException(string message, Exception inner) : base(message, inner)
        {
        }

        public IoException(EntryType type, BinaryReader reader) :
            base("Type mismatch in file. Read '" + Strings.ChrW((int)type) + "' at " + reader.BaseStream.Position)
        {
        }
    }

    public class Entry
    {
        public EntryType Type = EntryType.Empty;
        public object Value = "";
    }
}