// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.PixivApi
{
    public sealed class FileExtraInfoFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::PixivApi.FileExtraInfo>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::PixivApi.FileExtraInfo value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(6);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.Memo, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string[]>(formatterResolver).Serialize(ref writer, value.Tags, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::PixivApi.HideReason>(formatterResolver).Serialize(ref writer, value.HideReason, options);
            writer.Write(value.HideLast);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<uint, global::PixivApi.FilePageExtraInfo>>(formatterResolver).Serialize(ref writer, value.PageExtraInfoDictionary, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string[]>(formatterResolver).Serialize(ref writer, value.FakeTags, options);
        }

        public global::PixivApi.FileExtraInfo Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var ____result = new global::PixivApi.FileExtraInfo();

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        ____result.Memo = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 1:
                        ____result.Tags = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string[]>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 2:
                        ____result.HideReason = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::PixivApi.HideReason>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 3:
                        ____result.HideLast = reader.ReadBoolean();
                        break;
                    case 4:
                        ____result.PageExtraInfoDictionary = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<uint, global::PixivApi.FilePageExtraInfo>>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 5:
                        ____result.FakeTags = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string[]>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name
