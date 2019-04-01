using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Martin.Utils
{
    public class CSVReader : IDisposable
    {
        #region 字段

        // CSV文件读取流
        FileStream _fileStreamReader = null;
        // 文件已经读取到的位置
        int _fileStreamPos = 0;

        // CSV文件已经读取过的记录信息缓存
        List<RecordInfo> _recordInfos = new List<RecordInfo>();

        #endregion

        #region 属性

        bool _eof = false;
        /// <summary>
        /// 文件是否到达末尾
        /// </summary>
        public bool Eof
        {
            get { return _eof; }
        }

        Encoding _CSVEncoding;
        /// <summary>
        /// CSV文件编码方式
        /// </summary>
        public Encoding CSVEncoding
        {
            get { return _CSVEncoding; }
            set { _CSVEncoding = value; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// CSV文件读取器
        /// </summary>
        /// <param name="stream">指向CSV文件的流</param>
        public CSVReader(FileStream stream)
            : this(stream, Encoding.UTF8)
        {
        }

        /// <summary>
        /// CSV文件读取器
        /// </summary>
        /// <param name="stream">指向CSV文件的流</param>
        /// <param name="delimiter">CSV文件分隔符</param>
        /// <param name="encoding">CSV文件编码方式</param>
        public CSVReader(FileStream stream, Encoding encoding)
        {
            this._fileStreamReader = stream;
            this._CSVEncoding = encoding;
        }

        ~CSVReader()
        {
            Dispose(false);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据记录信息获取记录
        /// </summary>
        /// <param name="info">记录信息</param>
        /// <returns>记录</returns>
        private string ReadLineByRecordInfo(RecordInfo info)
        {
            byte[] tempBytes = new byte[info.RecordLength];
            this._fileStreamReader.Seek(info.RecordPosition, SeekOrigin.Begin);
            int bytesRead = this._fileStreamReader.Read(tempBytes, 0, tempBytes.Length);
            return this.CSVEncoding.GetString(tempBytes, 0, tempBytes.Length);
        }

        /// <summary>
        /// 读取记录
        /// </summary>
        /// <param name="start">记录开始位置</param>
        /// <returns></returns>
        private bool ReadLine(int start, out string record)
        {
            record = string.Empty;
            List<byte> tempBytes = new List<byte>();
            this._fileStreamReader.Seek(start, SeekOrigin.Begin);

            if (this._eof)
                return false;
            while (true)
            {
                //读取下一个字节
                int temp = this._fileStreamReader.ReadByte();
                if (temp == -1)
                {
                    //文件到达结尾
                    this._eof = true;
                    break;
                }
                else
                {
                    this._fileStreamPos += 1;
                    //换行符判断 13 \r 10 \n
                    if (temp == 13 || temp == 10)
                    {
                        if (temp == 13 && this._fileStreamReader.ReadByte() == 10)
                            this._fileStreamPos += 1;
                        break;
                    }
                    tempBytes.Add((byte)temp);
                }
            }

            if (tempBytes.Count > 0)
            {
                RecordInfo info = new RecordInfo(start, tempBytes.Count);
                this._recordInfos.Add(info);
                record = this.CSVEncoding.GetString(tempBytes.ToArray(), 0, tempBytes.Count);
                return true;
            }
            return false;
        }

        #endregion

        #region 公有方法

        /// <summary>
        /// 读取记录
        /// </summary>
        /// <param name="start">开始位置</param>
        /// <param name="count">偏移量</param>
        /// <returns></returns>
        public List<string> GetRecords(int start, int count)
        {
            List<string> records = new List<string>();

            int end = start + count;
            if (this._recordInfos.Count < start)
            {
                for (int i = this._recordInfos.Count; i < end; i++)
                {
                    string str = string.Empty;
                    if (this.ReadLine(this._fileStreamPos, out str))
                    {
                        if (i >= start)
                            records.Add(str);
                    }
                    else
                        break;
                }
            }
            else if (this._recordInfos.Count > end)
            {
                for (int i = start; i < end; i++)
                    records.Add(this.ReadLineByRecordInfo(this._recordInfos[i]));
            }
            else
            {
                for (int i = start; i < this._recordInfos.Count; i++)
                    records.Add(this.ReadLineByRecordInfo(this._recordInfos[i]));
                for (int i = this._recordInfos.Count; i < end; i++)
                {
                    string str = string.Empty;
                    if (this.ReadLine(this._fileStreamPos, out str))
                    {
                        if (i >= start)
                            records.Add(str);
                    }
                    else
                        break;
                }
            }

            return records;
        }

        public void Close()
        {
            this._fileStreamReader.Close();
            this._fileStreamReader.Dispose();
            this._fileStreamReader = null;
        }

        #endregion

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            //必须为true
            Dispose(true);
            //通知垃圾回收机制不再调用终结器（析构器）
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                // 清理托管资源
                this._recordInfos.Clear();
                this._recordInfos = null;
                if (_fileStreamReader != null)
                {
                    _fileStreamReader.Close();
                    _fileStreamReader.Dispose();
                    _fileStreamReader = null;
                }
            }
            // 清理非托管资源
            //
            //让类型知道自己已经被释放
            disposed = true;
        }

        #endregion 
    }

    /// <summary>
    /// CSV文件的一条记录的信息 一条记录即为一行CSV文本
    /// </summary>
    public class RecordInfo
    {
        int _recordPosition;
        /// <summary>
        /// 记录的起始位置
        /// </summary>
        public int RecordPosition
        {
            get { return _recordPosition; }
            set { _recordPosition = value; }
        }

        int _recordLength;
        /// <summary>
        /// 记录的长度
        /// </summary>
        public int RecordLength
        {
            get { return _recordLength; }
            set { _recordLength = value; }
        }

        public RecordInfo(int position, int length)
        {
            this.RecordPosition = position;
            this.RecordLength = length;
        }
    }
}
