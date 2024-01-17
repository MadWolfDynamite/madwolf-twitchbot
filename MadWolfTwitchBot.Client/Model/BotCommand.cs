using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
    public class BotCommand
    {
        public string Command { get; set; }
        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not BotCommand)
                return false;

            var command = obj as BotCommand;

            if (command.Command == null && command.Message == null)
                return command.Command == Command && command.Message == Message;
            if (command.Command == null && command.Message != null)
                return command.Command == Command && command.Message.Equals(Message);
            if (command.Command != null && command.Message == null)
                return command.Command.Equals(Command) && command.Message == Message;

            return command.Command.Equals(Command) && command.Message.Equals(Message);
        }

        public override int GetHashCode()
        {
            var commandHash = Command != null ? ShiftAndWrap((Command.Length - 1).GetHashCode(), 2) : 1;
            var messageHash = Message != null ? Message.Length.GetHashCode() : 1;

            return commandHash ^ messageHash;
        }

        private static int ShiftAndWrap(int value, int positions)
        {
            positions &= 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
    }
}
