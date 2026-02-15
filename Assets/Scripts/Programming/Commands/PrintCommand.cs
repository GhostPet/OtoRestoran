using System.Collections.Generic;
using UnityEngine;

public class PrintCommand : IRobotCommand {
    public int ExpectedArgumentCount => 1;

    public bool Tick(params object[] args) {
        int line = CommandExecutionContext.CurrentLine;

        if (args == null || args.Length != 1)
            throw new InvalidArgumentCountError("print", ExpectedArgumentCount, args?.Length ?? 0, line);

        var value = args[0];

        string outStr;
        if (value is List<float> list) {
            outStr = string.Join(",", list);
        } else if (value is float f) {
            outStr = f.ToString();
        } else if (value is int i) {
            outStr = i.ToString();
        } else if (value is string s) {
            outStr = s;
        } else if (value == null) {
            outStr = "null";
        } else {
            outStr = value.ToString();
        }

        Debug.Log($"[Print] (line {line}) {outStr}");
        return true;
    }

    public void Reset() { }
}
