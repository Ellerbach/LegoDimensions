// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace LegoDimensionsTests
{
    public class BufferSafetyTests
    {
        [Fact]
        public void LegoTagEventArgs_CopiesCardUidBuffer()
        {
            // Arrange
            byte[] source = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

            // Act
            var args = new LegoTagEventArgs(Pad.Center, true, source, null, 0);
            source[0] = 0xFF;

            // Assert
            Assert.Equal(0x01, args.CardUid[0]);
            Assert.NotSame(source, args.CardUid);
        }

        [Fact]
        public void PresentTag_CopiesCardUidBuffer()
        {
            // Arrange
            byte[] source = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70];

            // Act
            var presentTag = new PresentTag(Pad.Right, TagType.Normal, 2, source);
            source[0] = 0x99;

            // Assert
            Assert.Equal(0x10, presentTag.CardUid[0]);
            Assert.NotSame(source, presentTag.CardUid);
        }

        [Fact]
        public void Message_AddPayload_WritesUintBigEndian()
        {
            // Arrange
            var message = new Message(MessageCommand.None);

            // Act
            message.AddPayload((uint)0x01020304);
            var bytes = message.GetBytes(7);

            // Assert
            Assert.Equal((byte)MessageType.Normal, bytes[0]);
            Assert.Equal(6, bytes[1]);
            Assert.Equal((byte)MessageCommand.None, bytes[2]);
            Assert.Equal((byte)7, bytes[3]);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, bytes[4..8]);
        }

        [Fact]
        public void LegoPortal_Dispose_ClearsEventSubscription()
        {
            // Arrange
            var portal = (LegoPortal)RuntimeHelpers.GetUninitializedObject(typeof(LegoPortal));
            var eventField = typeof(LegoPortal)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(f => f.FieldType == typeof(EventHandler<LegoTagEventArgs>));
            Assert.NotNull(eventField);

            eventField!.SetValue(portal, (EventHandler<LegoTagEventArgs>?)((_, _) => { }));
            var cancelField = typeof(LegoPortal).GetField("_cancelThread", BindingFlags.Instance | BindingFlags.NonPublic);
            cancelField!.SetValue(portal, new CancellationTokenSource());

            var presentTagsField = typeof(LegoPortal).GetField("_presentTags", BindingFlags.Instance | BindingFlags.NonPublic);
            presentTagsField!.SetValue(portal, new List<PresentTag>());
            var padTagField = typeof(LegoPortal).GetField("_padTag", BindingFlags.Instance | BindingFlags.NonPublic);
            var padTagListType = typeof(LegoPortal).GetField("_padTag", BindingFlags.Instance | BindingFlags.NonPublic)!.FieldType;
            padTagField!.SetValue(portal, Activator.CreateInstance(padTagListType));
            var commandIdField = typeof(LegoPortal).GetField("_commandId", BindingFlags.Instance | BindingFlags.NonPublic);
            var commandIdListType = typeof(LegoPortal).GetField("_commandId", BindingFlags.Instance | BindingFlags.NonPublic)!.FieldType;
            commandIdField!.SetValue(portal, Activator.CreateInstance(commandIdListType));

            // Act
            portal.Dispose();

            // Assert
            Assert.Null(eventField.GetValue(portal));
        }
    }
}
