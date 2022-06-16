using System.Windows.Input;


namespace shared {
    //Keybind structures
    public enum ControlKey
    {
        None,
        Left_Joystick_Up,
        Left_Joystick_Left,
        Left_Joystick_Down,
        Left_Joystick_Right,
        Left_Joystick_Press,
        Right_Joystick_Up,
        Right_Joystick_Left,
        Right_Joystick_Down,
        Right_Joystick_Right,
        Right_Joystick_Press,
        Dpad_Up,
        Dpad_Left,
        Dpad_Down,
        Dpad_Right,
        Button_X,
        Button_Triangle,
        Button_Square,
        Button_Circle,
        Shoulder_Left,
        Shoulder_Right,
        Trigger_Left,
        Trigger_Right,
        Button_Share,
        Button_Options,
        Button_PS,
        TouchPad_Press
    }

    //used to filter keyboard input
    //also used to determine the size of an array with indices for each keycode
    //also holds key names for easier/quicker access
    public class keyFilter
    {
        int min = -1;    //keep track of lowest value key
        int max = -1;    //highest value key
        int offset; //value added to key when indexing array
        int length;     //range of key values. length of internal array
        int[] keyisValid;   //an integer value 0 or more is considered valid
        string[] keyNames;  //name for a given Key. indexed by result of keyisValid[Key]


        //marks the given keys valid and all others invalid 
        //names in optional string array should share the same index as their corresponding Key in keys
        public keyFilter(Key[] keys, string[] names = null)
        {
            //find min and max
            for (int i = 0; i < keys.Length; i++)
            {
                if (min == -1 || max == -1)
                {
                    min = (int)keys[i];
                    max = (int)keys[i];
                }
                else if ((int)keys[i] > max)
                {
                    max = (int)keys[i];
                }
                else if ((int)keys[i] < min)
                {
                    min = (int)keys[i];
                }
            }
            offset = 0 - min;
            length = (max - min) + 1;
            keyisValid = new int[length];
            keyNames = new string[keys.Length];
            for (int i = 0; i < length; i++)
            {
                keyisValid[i] = -1;     //-1 is the invalid value or really any negative
            }
            //set translation to be index into string array
            if ((names != null) && (names.Length == keys.Length))
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    keyisValid[(int)keys[i] + offset] = i;
                    keyNames[i] = names[i];
                }
            }
            else
            {  //no or invalid amount of names provided

                for (int i = 0; i < keys.Length; i++)
                {
                    keyisValid[(int)keys[i] + offset] = i;
                    keyNames[i] = keys[i].ToString();
                }
            }

        }
        public bool isValid(Key val)
        {
            if ((int)val < min || (int)val > max)
            {
                return false;
            }
            return (keyisValid[(int)val + offset] >= 0);
        }

        //produces a new Key array with only the valid characters remaining
        //somewhat slow due to the resizing. try to use isValid in your own loop if you can
        public Key[] apply(Key[] keys)
        {
            int newlen = 0;
            int curindex = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                if (isValid(keys[i]))
                {
                    newlen += 1;
                }
            }
            Key[] result = new Key[newlen];
            for (int i = 0; i < keys.Length; i++)
            {
                if (isValid(keys[i]))
                {
                    result[curindex] = keys[i];
                    curindex++;
                }
            }
            return result;
        }

        //length of internal translation array 
        public int getLength()
        {
            return length;
        }

        public int getOffset()
        {
            return offset;
        }

        public string getString(Key kval)
        {
            if (isValid(kval))
            {
                return keyNames[(int)(keyisValid[(int)kval + offset])];
            }
            return "";
        }
    }

    //Entry in conversion table for key to controller
    public class keybindArrayEntry
    {
        public ControlKey controlCode;
        public string controlName;
        public Key keyCode;
        public string keyName;

        public keybindArrayEntry(ControlKey cCode, Key kCode, string cName = null, string kName = null)
        {
            controlCode = cCode;
            keyCode = kCode;

            if (cName != null && cName != "")
                controlName = cName;
            else
                controlName = cCode.ToString();

            if (kName != null && kName != "")
                keyName = kName;
            else
                keyName = kCode.ToString();
        }

        //soft copy. same internal memory references as copied
        public keybindArrayEntry(keybindArrayEntry copy)
        {
            controlCode = copy.controlCode;
            controlName = copy.controlName;
            keyCode = copy.keyCode;
            keyName = copy.keyName;
        }
    }

    //conversion table for key to controller
    public class keybindArray
    {
        keybindArrayEntry[] data;

        public keybindArray(int len)
        {
            data = new keybindArrayEntry[len];
        }
        //array constructor is by reference
        public keybindArray(keybindArrayEntry[] arr)
        {
            data = arr;
        }
        //copy constructor is deep
        public keybindArray(keybindArray arr)
        {
            data = new keybindArrayEntry[arr.getLength()];
            deepCopy(data, arr.data);
        }

        public keybindArrayEntry getEntry(int index)
        {
            return data[index];
        }

        public void deepCopy(keybindArrayEntry[] dest, keybindArrayEntry[] src)
        {
            if (dest.Length != src.Length)
                dest = new keybindArrayEntry[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dest[i] = new keybindArrayEntry(src[i].controlCode, src[i].keyCode, string.Copy(src[i].controlName), string.Copy(src[i].keyName));
            }
        }

        public void deepCopy(keybindArray src)
        {
            deepCopy(data, src.data);
        }

        public int getLength()
        {
            return data.Length;
        }


        /*public bool setControl(string controlName, Key val, string keyName=null) {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].controlName == controlName)
                {
                    data[i].keyCode = val;
                    if (keyName != null && keyName != "")
                        data[i].keyName = keyName;
                    else
                        data[i].keyName = val.ToString();
                    return true;
                }
            }
            return false;
        }
        */
    }

    //used for much faster mapping (key -> control), but not easily changed
    //settable filter to filter out invalid keys
    public class KeybindTranslator
    {
        ControlKey[][] translationMap;
        public keyFilter filter;
        public bool isError = false;
        KeybindTranslator(keybindArray kbarr, keyFilter kf)
        {
            filter = kf;
            translationMap = new ControlKey[filter.getLength()][];
            ControlKey[] tempArr;
            Key curkey;
            int curlen;
            int mapIndex;
            for (int i = 0; i < kbarr.getLength(); i++)
            {
                curkey = kbarr.getEntry(i).keyCode;
                mapIndex = (int)curkey + filter.getOffset();
                if (filter.isValid(curkey))
                {
                    tempArr = translationMap[mapIndex];
                    curlen = tempArr.Length;
                    if (tempArr == null)
                    {
                        tempArr = new ControlKey[1];
                    }
                    else
                    { //counting on not too many keybind overlaps
                        translationMap[mapIndex] = new ControlKey[curlen + 1];
                        tempArr.CopyTo(translationMap[i], 0);
                    }
                    translationMap[mapIndex][curlen] = kbarr.getEntry(i).controlCode;
                }
                else
                {
                    isError = true;
                }
            }
        }

        ControlKey[] translate(Key val)
        {
            if (!filter.isValid(val))
            {
                isError = true;
                return new ControlKey[0];
            }
            return translationMap[(int)val + filter.getOffset()];
        }
    }

}