using System.Text;

namespace AI.DiffAssistant.Core.File;

/// <summary>
/// 自动检测文件编码（UTF-8、GBK、ASCII）
/// </summary>
public static class EncodingDetector
{
    /// <summary>
    /// 检测文件编码
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>检测到的编码</returns>
    public static Encoding Detect(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new System.IO.FileNotFoundException($"文件不存在: {filePath}");
        }

        using var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
        return Detect(stream);
    }

    /// <summary>
    /// 检测字节流的编码
    /// </summary>
    /// <param name="stream">文件流（需要支持 Seek）</param>
    /// <returns>检测到的编码</returns>
    public static Encoding Detect(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException(nameof(stream));
        }

        // 保存当前位置
        var originalPosition = stream.Position;

        try
        {
            // 读取足够的字节进行检测
            var buffer = new byte[Math.Min(stream.Length, 4096)];
            stream.Read(buffer, 0, buffer.Length);
            stream.Position = 0;

            return DetectEncoding(buffer);
        }
        finally
        {
            // 恢复流位置
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// 检测字节数组的编码
    /// </summary>
    private static Encoding DetectEncoding(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
        {
            return Encoding.Default;
        }

        // 1. 检查 UTF-8 BOM (EF BB BF)
        if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        // 2. 检查 UTF-16 LE BOM (FF FE)
        if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        // 3. 检查 UTF-16 BE BOM (FE FF)
        if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        // 4. 检查 UTF-32 LE BOM (FF FE 00 00)
        if (buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
        {
            return new UTF32Encoding(true, true);
        }

        // 5. 检查 UTF-32 BE BOM (00 00 FE FF)
        if (buffer.Length >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
        {
            return new UTF32Encoding(false, true);
        }

        // 6. 首先检查是否为纯 ASCII（优先返回 ASCII）
        // 注意：纯 ASCII 文件实际上是有效的 UTF-8，但为了更准确的检测，
        // 我们在无法确定是 UTF-8 还是 ASCII 时，默认返回 UTF-8
        if (IsPureAscii(buffer))
        {
            // 纯 ASCII 同时也是有效的 UTF-8，默认返回 UTF-8
            return Encoding.UTF8;
        }

        // 7. 无 BOM，尝试检测是否为有效的 UTF-8
        if (IsValidUtf8(buffer))
        {
            return Encoding.UTF8;
        }

        // 8. 尝试检测是否为 GBK（中文 Windows 常见）
        if (IsGbkLike(buffer))
        {
            try
            {
                return Encoding.GetEncoding("GBK");
            }
            catch
            {
                // GBK 编码不可用时回退到 UTF-8
                return Encoding.UTF8;
            }
        }

        // 9. 最终回退到系统默认编码
        return Encoding.Default;
    }

    /// <summary>
    /// 检查是否为有效的 UTF-8（无 BOM）
    /// </summary>
    private static bool IsValidUtf8(byte[] buffer)
    {
        int continuationCount = 0;

        foreach (byte b in buffer)
        {
            if (continuationCount > 0)
            {
                // 检查是否为 10xxxxxx 格式
                if ((b & 0xC0) != 0x80)
                {
                    return false;
                }
                continuationCount--;
            }
            else
            {
                if ((b & 0x80) == 0x00)
                {
                    // 0xxxxxxx - ASCII
                    continue;
                }
                if ((b & 0xE0) == 0xC0)
                {
                    // 110xxxxx - 2 字节序列
                    continuationCount = 1;
                }
                else if ((b & 0xF0) == 0xE0)
                {
                    // 1110xxxx - 3 字节序列
                    continuationCount = 2;
                }
                else if ((b & 0xF8) == 0xF0)
                {
                    // 11110xxx - 4 字节序列
                    continuationCount = 3;
                }
                else
                {
                    // 无效的 UTF-8 起始字节
                    return false;
                }
            }
        }

        return continuationCount == 0;
    }

    /// <summary>
    /// 检查是否包含中文字符（可能是 GBK 编码）
    /// </summary>
    private static bool IsGbkLike(byte[] buffer)
    {
        // GBK 编码的中文范围：0x8140-0xFEFE（简化检测）
        // 检查是否有连续的高位字节，可能表示中文字符
        int chineseLikeSequences = 0;

        for (int i = 0; i < buffer.Length - 1; i++)
        {
            byte b1 = buffer[i];
            byte b2 = buffer[i + 1];

            // 检测可能是 GBK 中文的模式（双字节，高位都为 1）
            if ((b1 & 0x80) != 0 && (b2 & 0x80) != 0)
            {
                chineseLikeSequences++;

                // 如果连续发现多个，可能确实是中文编码
                if (chineseLikeSequences >= 3)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查是否为纯 ASCII
    /// </summary>
    private static bool IsPureAscii(byte[] buffer)
    {
        foreach (byte b in buffer)
        {
            if ((b & 0x80) != 0)
            {
                return false;
            }
        }
        return true;
    }
}
