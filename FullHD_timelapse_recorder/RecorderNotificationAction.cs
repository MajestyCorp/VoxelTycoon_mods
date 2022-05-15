using VoxelTycoon.Notifications;
using VoxelTycoon.Serialization;
using VoxelTycoon.UI;

namespace TimelapseMod
{
    class RecorderNotificationAction : INotificationAction
    {
        private string _filepath;

        public void SetPath(string path)
        {
            _filepath = path;
        }
        public void Act()
        {
            FileUtils.Reveal(_filepath);
        }

        public void Read(StateBinaryReader reader)
        {
            _filepath = reader.ReadString();
        }

        public void Write(StateBinaryWriter writer)
        {
            writer.WriteString(_filepath);
        }
    }
}
