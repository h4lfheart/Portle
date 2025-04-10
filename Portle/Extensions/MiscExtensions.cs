﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DynamicData.Binding;

namespace Portle.Extensions;

public static class MiscExtensions
{
    public static bool Filter(string input, string filter)
    {
        var filters = filter.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    public static bool FilterAll(string input, IEnumerable<string> filters)
    {
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    public static bool FilterAny(string input, IEnumerable<string> filters)
    {
        return filters.Any(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    public static bool AddUnique<T>(this List<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }

    public static bool AddUnique<T>(this ObservableCollection<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }

    public static bool AddUnique<T, K>(this IDictionary<T, K> dict, T key, K value)
    {
        if (dict.ContainsKey(key)) return false;
        dict.Add(key, value);
        return true;
    }

    public static bool AddUnique<T, K>(this IDictionary<T, K> dict, KeyValuePair<T, K> kvp)
    {
        if (dict.ContainsKey(kvp.Key)) return false;
        dict.Add(kvp.Key, kvp.Value);
        return true;
    }

    public static string CommaJoin<T>(this IEnumerable<T> enumerable, bool includeAnd = true)
    {
        var list = enumerable.ToList();
        var joiner = includeAnd ? list.Count == 2 ? " and " : ", and " : ", ";
        return list.Count > 1 ? string.Join(", ", list.Take(list.Count - 1)) + joiner + list.Last() : list.First().ToString();
    }

    public static byte[] ReadToEnd(this Stream str)
    {
        var bytes = new BinaryReader(str).ReadBytes((int) str.Length);
        str.Position = 0;
        return bytes;
    }

    public static byte[] StringToBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static string BytesToString(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Select((i, val) => (val, i));
    }

    public static bool AddIfNotNull<T>(this List<T> list, T? obj)
    {
        if (obj is null) return false;
        list.Add(obj);
        return true;
    }
    
    public static void AddRangeIfNotNull<T>(this List<T> list, IEnumerable<T?> items)
    {
        foreach (var item in items)
        {
            if (item is null) continue;
            list.Add(item);
        }
    }

    public static IEnumerable<T> RemoveNull<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Where(x => x is not null);
    }

    public static string GetHash(this Stream stream)
    {
        return BitConverter.ToString(SHA256.HashData(stream.ReadToEnd())).Replace("-", string.Empty);
    }
    
    public static string GetHash(this FileInfo fileInfo)
    {
        return BitConverter.ToString(SHA256.HashData(File.ReadAllBytes(fileInfo.FullName))).Replace("-", string.Empty);
    }
    
    public static T? Random<T>(this IEnumerable<T> enumerable)
    {
        var list = enumerable.ToList();
        if (list.Count == 0) return default;
        
        var index = System.Random.Shared.Next(0, list.Count);
        return list[index];
    }
    
    public static bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static void ForEach<T>(this IEnumerable<T> array, Action<T> action)
    {
        foreach (var item in array)
        {
            action(item);
        }
    }

    public static int IndexOf<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
    {
        var array = enumerable.ToArray();
        for (var i = 0; i < array.Length; i++)
        {
            if (predicate(array[i])) return i;
        }

        return -1;
    }

    public static int RemoveAll<T>(this IList<T> list, Predicate<T> predicate)
    {
        var removed = 0;
        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
    
    public static void InsertSorted<T>(this IList<T> list, T item, SortExpressionComparer<T> comparer) 
    {
        list.Add(item);
        var i = list.Count-1;
        for ( ; i > 0 && comparer.Compare(list[i-1], item) < 0 ; i--) {
            list[i] = list[i-1];
        }
        list[i] = item;
    }

    public static T CreateValue<T>(this Lazy<T> lazy)
    {
        return lazy.Value;
    }

    public static T CreateXaml<T>(this string xaml, dynamic bindings) where T : Control
    {
        var content = AvaloniaRuntimeXamlLoader.Parse<T>(xaml);
        content.DataContext = bindings;
        return content;
    }
    
    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        Copy(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
    }

    public static void Copy(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        foreach (var subDirectory in source.GetDirectories())
        {
            Copy(subDirectory, target.CreateSubdirectory(subDirectory.Name));
        }
    }
    
    public static bool IsProcessRunning(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.Any(process => process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
    }
    
    public static Process? GetRunningProcess(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.FirstOrDefault(process => process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}