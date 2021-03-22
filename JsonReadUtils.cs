using System.Text.Json;
using System;
using FastEnumUtility;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public static class JsonReadUtils
{
    private static void ThrowJsonException(string message)
    {
        throw new JsonException(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateArrayStartImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data 3");
        if (json.TokenType != JsonTokenType.StartArray) ThrowJsonException("Invalid token type, expected start of array");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateArrayEndtImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.EndArray) ThrowJsonException("Invalid token type, expected end of array");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidatePropertyImpl(ref Utf8JsonReader json, string propKey, bool skipRead = false)
    {
        if (!skipRead && !json.Read()) ThrowJsonException("Failed to read data 2");
        if (json.TokenType != JsonTokenType.PropertyName) ThrowJsonException($"Invalid token type, expected property name {propKey} {json.TokenType}");
        if (!json.ValueTextEquals(propKey)) ThrowJsonException("Invalid Property name");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadStringImpl(ref Utf8JsonReader json, bool skipRead = false)
    {
        if (!skipRead && !json.Read()) ThrowJsonException("Failed to read data 1");
        if (json.TokenType != JsonTokenType.String) ThrowJsonException("Invalid token type, expected string");

        return json.GetString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadInt32Impl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadUInt16Impl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetUInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static short ReadInt16Impl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte ReadSByteImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetSByte();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ReadByteImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadFloatImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

        return json.GetSingle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ReadBoolImpl(ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.True || json.TokenType != JsonTokenType.False) ThrowJsonException("Invalid token type, expected bool");

        return json.GetBoolean();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadObjectStart(this ref Utf8JsonReader json, bool skipRead = false)
    {
        if (!skipRead && !json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.StartObject) ThrowJsonException("Invalid token type, expected start of object");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadObjectEnd(this ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.EndObject) ThrowJsonException("Invalid token type, expected start of array");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadPropertyName(this ref Utf8JsonReader json)
    {
        if (!json.Read()) ThrowJsonException("Failed to read data");
        if (json.TokenType != JsonTokenType.PropertyName) ThrowJsonException($"Invalid token type, expected property name found {json.TokenType}");
        return json.GetString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadByteImpl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadSByteImpl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadInt16(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadInt16Impl(ref json);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadUInt16Impl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadInt32Impl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadFloat(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadFloatImpl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBool(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return ReadBoolImpl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadString(this ref Utf8JsonReader json, string key, bool skipFirstRead = false)
    {
        ValidatePropertyImpl(ref json, key, skipFirstRead);
        return ReadStringImpl(ref json);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ReadDateTime(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        return DateTime.Parse(ReadStringImpl(ref json));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadEnum<T>(this ref Utf8JsonReader json, string key) where T : struct, Enum
    {
        ValidatePropertyImpl(ref json, key);

        var str = ReadStringImpl(ref json);
        FastEnum.TryParse(str, out T enumVal);

        return enumVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadEnumArray<T>(this ref Utf8JsonReader json, string key, List<T> outData) where T : struct, Enum
    {
        ValidatePropertyImpl(ref json, key);
        ValidateArrayStartImpl(ref json);

        while (json.Read() && json.TokenType != JsonTokenType.EndArray)
        {
            if (json.TokenType != JsonTokenType.String) ThrowJsonException("Invalid token type, expected string");

            var str = json.GetString();
            FastEnum.TryParse(str, out T enumVal);
            outData.Add(enumVal);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArrayStart(this ref Utf8JsonReader json, string key)
    {
        ValidatePropertyImpl(ref json, key);
        ValidateArrayStartImpl(ref json);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStringArray(this ref Utf8JsonReader json, string key, List<string> outData)
    {
        ValidatePropertyImpl(ref json, key);
        ValidateArrayStartImpl(ref json);

        while (json.Read() && json.TokenType != JsonTokenType.EndArray)
        {
            if (json.TokenType != JsonTokenType.String) ThrowJsonException("Invalid token type, expected string");

            outData.Add(json.GetString());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFloatArray(this ref Utf8JsonReader json, string key, List<float> outData)
    {
        ValidatePropertyImpl(ref json, key);
        ValidateArrayStartImpl(ref json);

        while (json.Read() && json.TokenType != JsonTokenType.EndArray)
        {
            if (json.TokenType != JsonTokenType.Number) ThrowJsonException("Invalid token type, expected Number");

            outData.Add(json.GetSingle());
        }
    }
}
