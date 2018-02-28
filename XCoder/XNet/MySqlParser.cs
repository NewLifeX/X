using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using NewLife.Data;

namespace XCoder.XNet
{
    #region 分析器
    abstract class MySqlParser
    {
        public Action<MySqlPacket> OnParsedPacket;
        public Action OnParsedError;

        public DateTime LastTime { get; set; }
        public IPEndPoint LastSrcEP { get; set; }
        public IPEndPoint LastDstEP { get; set; }

        public abstract void Parse(Packet pk);

        //The Packet Header
        //Bytes                 Name
        // -----                 ----
        // 3                     Packet Length
        // 1                     Packet Number

        // Packet Length: The length, in bytes, of the packet
        //                that follows the Packet Header. There
        //                may be some special values in the most
        //                significant byte. The maximum packet 
        //                length is (2**24 -1),about 16MB.

        // Packet Number: A serial number which can be used to
        //                ensure that all packets are present
        //                and in order. The first packet of a
        //                client query will have Packet Number = 0
        //                Thus, when a new SQL statement starts, 
        //                the packet number is re-initialised.

        protected void ReadPacketHeader(Byte[] header, out Int32 packetLength, out Int32 packetNum)
        {
            if (header == null || header.Length != 4) throw new ArgumentException();

            packetLength = header[0] + (header[1] << 8) + (header[3] << 16);
            packetNum = header[3];
        }
    }

    class MySqlQueryParser : MySqlParser
    {
        private enum ParsingState
        {
            Head,
            Body
        }

        private ParsingState _state;
        private MySqlQueryPacket _packet;

        public MySqlQueryParser()
        {
            _state = ParsingState.Head;
            _packet = new MySqlQueryPacket();
        }

        public override void Parse(Packet pk)
        {
            TryParse(pk);
        }

        private void TryParse(Packet pk)
        {
            if (_state == ParsingState.Head)
            {
                if (pk.Count >= 4)
                {
                    var head = pk.ReadBytes(4);
                    ReadPacketHeader(head, out _packet.Length, out _packet.Num);
                    _state = ParsingState.Body;
                }
                else
                    return;
            }

            if (_state == ParsingState.Body)
            {
                if (pk.Count >= _packet.Length)
                {
                    var body = _packet.Body = pk.ReadBytes(_packet.Length);
                    _packet.Command = (DBCmd)body[0];
                    HandleParseOk();
                }
            }
        }

        private void HandleParseError(String msg)
        {
            _state = ParsingState.Head;
            _packet = new MySqlQueryPacket();

            OnParsedError?.Invoke();
        }

        private void HandleParseOk()
        {
            if (OnParsedPacket != null)
            {
                _packet.Time = LastTime;
                _packet.SrcEP = LastSrcEP;
                _packet.DstEP = LastDstEP;
                OnParsedPacket(_packet);
            }
            _state = ParsingState.Head;
            _packet = new MySqlQueryPacket();
        }
    }
    #endregion

    #region 数据包
    enum PacketType
    {
        Query,
        ResultSet,
    }

    class MySqlPacket
    {
        public DateTime Time { get; set; }
        public IPEndPoint SrcEP { get; set; }
        public IPEndPoint DstEP { get; set; }

        public PacketType PacketType { get; set; }
    }
    #endregion

    #region 结果包
    enum ResultSetType
    {
        OK,
        Error,
        ResultSet,
    }

    class MySqlResultSetPacket : MySqlPacket
    {
        public ResultSetType ResultSetType { get; set; }

        public MySqlResultSetPacket()
        {
            PacketType = PacketType.ResultSet;
        }
    }

    class MySqlOKResultSet : MySqlResultSetPacket
    {
        public Int32 Length;
        public Int32 Num;
        public Int32 AffectRow;
        public Int32 ServerState;
        public String Message;

        public MySqlOKResultSet()
        {
            ResultSetType = ResultSetType.OK;
        }
    }

    class MySqlErrorResultSet : MySqlResultSetPacket
    {
        public Int32 Length;
        public Int32 Num;
        public Int32 ErrorNum;
        public Int32 SqlState;
        public String Message;

        public MySqlErrorResultSet()
        {
            ResultSetType = ResultSetType.Error;
        }
    }

    class MySqlDataResultSet : MySqlResultSetPacket
    {
        public ResultSetHeadPacket HeadPacket;
        public List<ResultSetFieldPacket> FieldPacket;
        public List<ResultSetRowPacket> RowPacket;

        public MySqlDataResultSet()
        {
            ResultSetType = ResultSetType.ResultSet;
        }
    }

    public class ResultSetHeadPacket
    {
        public Int32 PacketLength;
        public Int32 PacketNum;
        public Int32 FieldNum;
    }

    public class ResultSetFieldPacket
    {
        public Int32 PacketLength;
        public Int32 PacketNum;
    }

    public class ResultSetRowPacket
    {
        public Int32 PacketLength;
        public Int32 PacketNum;
    }
    #endregion

    #region 请求包
    class MySqlQueryPacket : MySqlPacket
    {
        public Int32 Length;
        public Int32 Num;
        public Byte[] Body;
        public DBCmd Command;

        private String _commandArgs;
        public String CommandArgs
        {
            get
            {
                if (Command == DBCmd.QUERY && Body != null && Body.Length > 0)
                {
                    if (_commandArgs == null)
                        _commandArgs = Encoding.UTF8.GetString(Body, 1, Body.Length - 1);
                }

                return _commandArgs;
            }
        }

        public MySqlQueryPacket()
        {
            PacketType = PacketType.Query;
        }

        public MySqlCommandArgs ResolveCommand()
        {
            var args = new MySqlCommandArgs
            {
                CmdType = CommandType.Text
            };

            var cmd = CommandArgs;
            if (cmd != null)
            {
                if (cmd.Substring(0, 4) == "CALL")
                {
                    args.CmdType = CommandType.StoredProcedure;

                    var start = cmd.IndexOf("(");
                    if (start > 3) args.ResolveQuery = cmd.Substring(3, start - 3);
                }
                else
                {
                    //todo replace ='xx' to ={}
                }
            }

            return args;
        }
    }

    class MySqlCommandArgs
    {
        public CommandType CmdType { get; set; }
        public String ResolveQuery { get; set; }
    }

    enum DBCmd : Byte
    {
        SLEEP = 0,
        QUIT = 1,
        INIT_DB = 2,
        QUERY = 3,
        FIELD_LIST = 4,
        CREATE_DB = 5,
        DROP_DB = 6,
        RELOAD = 7,
        SHUTDOWN = 8,
        STATISTICS = 9,
        PROCESS_INFO = 10,
        CONNECT = 11,
        PROCESS_KILL = 12,
        DEBUG = 13,
        PING = 14,
        TIME = 15,
        DELAYED_INSERT = 16,
        CHANGE_USER = 17,
        BINLOG_DUMP = 18,
        TABLE_DUMP = 19,
        CONNECT_OUT = 20,
        REGISTER_SLAVE = 21,
        PREPARE = 22,
        EXECUTE = 23,
        LONG_DATA = 24,
        CLOSE_STMT = 25,
        RESET_STMT = 26,
        SET_OPTION = 27,
        FETCH = 28
    }
    #endregion
}