﻿using NewLife.Data;
using NewLife.Model;
using NewLife.Net;

namespace NewLife.MQTT.ProxyProtocol;

/// <summary>ProxyProtocol编码器</summary>
public class ProxyCodec : Handler
{
    /// <summary>解析数据包，如果是ProxyProtocol协议则解码后返回</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is IPacket pk && context is NetHandlerContext ctx && ctx.Data is ReceivedEventArgs e)
        {
            var data = pk.GetSpan();
            if (ProxyMessage.FastValidHeader(data))
            {
                var msg = new ProxyMessage();
                var rs = msg.Read(data);
                if (rs > 0)
                {
                    if (context is IExtend ext)
                    {
                        ext["Proxy"] = msg;

                        // 修改远程地址
                        e.Remote = msg.GetClient().EndPoint;
                    }

                    message = pk.Slice(rs);
                }
            }
            else if (ProxyMessageV2.FastValidHeader(data))
            {
                var msg = new ProxyMessageV2();
                var rs = msg.Read(data);
                if (rs > 0)
                {
                    if (context is IExtend ext)
                    {
                        ext["Proxy"] = msg;

                        // 修改远程地址
                        e.Remote = msg.Client!.EndPoint;
                    }

                    message = pk.Slice(rs);
                }
            }
        }

        return base.Read(context, message);
    }

    /// <summary>编码数据包，加上ProxyProtocol头部</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        if (message is IPacket pk && context.Owner is ISocketRemote remote)
        {
            var msg = new ProxyMessage
            {
                Protocol = "TCP4",
                ClientIP = remote.Local.Address + "",
                ClientPort = remote.Local.Port,
                ProxyIP = remote.Remote.Address + "",
                ProxyPort = remote.Remote.Port,
            };

            var ap = new ArrayPacket(msg.ToPacket().GetBytes());
            //ap.Append(pk);
            ap.Next = pk;

            return base.Write(context, ap);
        }

        return base.Write(context, message);
    }
}