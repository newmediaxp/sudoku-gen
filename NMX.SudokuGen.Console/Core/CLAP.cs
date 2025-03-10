namespace NMX.CLAP
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Command Line Arguments Processor
    /// </summary>
    public sealed class CLAP
    {
        public readonly string[] commands;
        public readonly Dictionary<string, bool> flags;
        public readonly Dictionary<string, string?> flagsWithValue;
        public readonly List<string> values = [];

        public CLAP(in string[] p_commands, in string[] p_flags, in string[] p_flagsWithValue)
        {
            commands = p_commands;
            flags = new(p_flags.Length);
            foreach (string a_flag in p_flags) flags.Add(a_flag, false);
            flagsWithValue = new(p_flagsWithValue.Length);
            foreach (string a_flagWithValue in p_flagsWithValue) flagsWithValue.Add(a_flagWithValue, null);
        }

        public void Clear()
        {
            foreach (string a_key in flags.Keys) flags[a_key] = false;
            foreach (string a_key in flagsWithValue.Keys) flagsWithValue[a_key] = null;
            values.Clear();
        }

        public (bool success, string cmd_or_msg) Process(in string[] p_inputs)
        {
            if (p_inputs == null) return (false, "no inputs");
            string a_input;
            string? a_command = null;
            for (int i = 0; i < p_inputs.Length; ++i)
            {
                a_input = p_inputs[i];
                if (string.IsNullOrEmpty(a_input)) continue;
                a_input = a_input.ToLower();
                //-- extract command
                if (commands.Contains(a_input))
                {
                    if (a_command != null) return (false, "multiple commands");
                    a_command = a_input; continue;
                }
                //-- extract flags
                if (flags.TryGetValue(a_input, out bool a_found))
                {
                    if (a_found) return (false, $"duplicate flag '{a_input}'");
                    flags[a_input] = true; continue;
                }
                //-- extract flags with value
                if (i + 1 < p_inputs.Length && !string.IsNullOrEmpty(p_inputs[i + 1])
                    && flagsWithValue.TryGetValue(a_input, out string? a_value))
                {
                    if (a_value != null) return (false, $"duplicate flag with value '{a_input}'");
                    flagsWithValue[a_input] = p_inputs[++i]; continue;
                }
                //-- extract value
                if (a_command != null) values.Add(a_input);
            }
            if (a_command == null) return (false, $"no command");
            return (true, a_command);
        }
    }
}
