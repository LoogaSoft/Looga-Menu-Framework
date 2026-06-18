using System;

namespace LoogaSoft.Menu
{
    [Serializable]
    public struct LoogaBlackboardValue
    {
        public LoogaBlackboardValueType type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;

        public static LoogaBlackboardValue Bool(bool value)
        {
            return new LoogaBlackboardValue
            {
                type = LoogaBlackboardValueType.Bool,
                boolValue = value
            };
        }

        public static LoogaBlackboardValue Int(int value)
        {
            return new LoogaBlackboardValue
            {
                type = LoogaBlackboardValueType.Int,
                intValue = value
            };
        }

        public static LoogaBlackboardValue Float(float value)
        {
            return new LoogaBlackboardValue
            {
                type = LoogaBlackboardValueType.Float,
                floatValue = value
            };
        }

        public static LoogaBlackboardValue String(string value)
        {
            return new LoogaBlackboardValue
            {
                type = LoogaBlackboardValueType.String,
                stringValue = value
            };
        }
    }
}
