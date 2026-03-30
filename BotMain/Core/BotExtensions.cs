using BotMsg = BotMain.Message;
using NapCoreData = NapPlana.Core.Data;
using NapMsg = NapPlana.Core.Data.Message;

namespace BotMain.Core;

/// <summary>NapPlana 库枚举向 Bot 内部枚举的转换扩展</summary>
internal static class BotExtensions
{
    internal static PrivateMessageSubType ToBotSubType(this NapCoreData.PrivateMessageSubType subType)
    {
        return subType switch
        {
            NapCoreData.PrivateMessageSubType.Friend => PrivateMessageSubType.Friend,
            NapCoreData.PrivateMessageSubType.Group  => PrivateMessageSubType.Temporary,
            NapCoreData.PrivateMessageSubType.Other  => PrivateMessageSubType.Other,
            _                                        => PrivateMessageSubType.None
        };
    }

    internal static GroupRole ToBotGroupRole(this NapCoreData.GroupRole role)
    {
        return role switch
        {
            NapCoreData.GroupRole.Owner  => GroupRole.Owner,
            NapCoreData.GroupRole.Admin  => GroupRole.Admin,
            NapCoreData.GroupRole.Member => GroupRole.Member,
            _                           => GroupRole.None
        };
    }

    internal static NapMsg.MessageBase ToNapMessage(this BotMsg.MessageBase msg)
    {
        switch (msg)
        {
            case BotMsg.TextMessage tm:
            {
                var nap = new NapMsg.TextMessage();
                ((NapMsg.TextMessageData)nap.MessageData).Text =
                    ((BotMsg.TextMessageData)tm.MessageData).Text;
                return nap;
            }
            case BotMsg.AtMessage am:
            {
                var nap = new NapMsg.AtMessage();
                ((NapMsg.AtMessageData)nap.MessageData).Qq =
                    ((BotMsg.AtMessageData)am.MessageData).Qq;
                return nap;
            }
            case BotMsg.ReplyMessage rm:
            {
                var nap = new NapMsg.ReplyMessage();
                ((NapMsg.ReplyMessageData)nap.MessageData).Id =
                    ((BotMsg.ReplyMessageData)rm.MessageData).Id;
                return nap;
            }
            case BotMsg.ImageMessage im:
            {
                var src = (BotMsg.ImageMessageData)im.MessageData;
                var nap = new NapMsg.ImageMessage();
                var dst = (NapMsg.ImageMessageData)nap.MessageData;
                dst.Name       = src.Name;
                dst.Summary    = src.Summary;
                dst.File       = src.File;
                dst.SubType    = src.SubType;
                dst.FileId     = src.FileId;
                dst.Url        = src.Url;
                dst.Path       = src.Path;
                dst.FileSize   = src.FileSize;
                dst.FileUnique = src.FileUnique;
                return nap;
            }
            default:
                return new NapMsg.MessageBase();
        }
    }
}
