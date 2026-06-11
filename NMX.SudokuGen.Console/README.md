# Sudoku Gen Console

`sudoku` `C#` `app` `cli`

&copy; New Media XP  
|
[website](https://www.newmediaxp.com)
|
[email](mailto:contact@newmediaxp.com)
|

---

## Description

A CLI application written in C# that can generate and solve sudoku puzzles.

---

## Usage

Commands: `create`, `solve`, `shuffle`, `version`, `help`

| Flag      | Meaning                                   | Commands               |
|-----------|-------------------------------------------|------------------------|
| `-r`      | rank of sudoku, 2 to 5 (4 ≈ 0.1s; 5 takes minutes) | create        |
| `-b`      | desired blanks (default rank³; 0 = solved grid; `max` = as many as possible) | create |
| `-t`      | number of sudokus                         | create                 |
| `-s`      | seed; same seed reproduces same result    | create, shuffle        |
| `-i`      | input .txt file (one puzzle code a line)  | solve, shuffle         |
| `-o`      | output .txt file (appended)               | create, solve, shuffle |
| `-omin`   | minimal output                            | create, solve, shuffle |
| `-oboard` | draw output as a formatted board          | create, solve, shuffle |
| `-osoln`  | include the solution even in minimal output | create, shuffle      |

Examples:

```
SudokuGen create
SudokuGen create -r 3 -b 40 -t 10 -o ./outputs.txt
SudokuGen create -s 20260610
SudokuGen solve 390560027.001809405.600172893.180257900.500643000.204081570.003096052.050328619.962010300
SudokuGen solve -i ./inputs.txt -o ./outputs.txt -oboard
SudokuGen shuffle -i ./inputs.txt -s 777
```

Puzzle code format: one character per square, row by row — `0` = blank,
`1`-`9` as digits, `10`+ as letters (`A` = 10), `.` between rows.
Exit code is 0 on success, 1 when any argument or puzzle failed; errors print to stderr.

---

## Details

* Released on *3 April 2025*

* Dependencies

    * .Net 10.0
    * Sudoku Gen Library [(link)](../NMX.SudokuGen.Library)

* Downloads

    * [Github](https://github.com/newmediaxp/sudoku-gen/releases)

---

## Contact

* Link: https://www.newmediaxp.com/contact
* Email-1: <contact@newmediaxp.com>
* Email-2: <animaxneil@gmail.com>

---
