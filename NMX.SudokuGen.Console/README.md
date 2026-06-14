# Sudoku Gen Console

`sudoku` `C#` `app` `cli`

&copy; 2024-2026 New Media XP. Licensed under the [MIT License](../LICENSE).  
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

A bare launch with no arguments prints the usage info.

### Commands

| Command   | Aliases           | Meaning                                              |
|-----------|-------------------|------------------------------------------------------|
| `create`  | —                 | Creates a sudoku. Default rank = 3, blanks = rank^3. |
| `solve`   | —                 | Solves a sudoku.                                     |
| `shuffle` | —                 | Shuffles a sudoku into an equivalent variant.        |
| `version` | `--version`, `-v` | Shows the app version.                               |
| `help`    | `--help`, `-h`    | Shows the usage info.                                |

### Flags with value

| Flag | Meaning                                    | Value                                     | Commands                     |
|------|--------------------------------------------|-------------------------------------------|------------------------------|
| `-r` | rank of sudoku                             | { r \| r ∈ N, 2 <= r <= 5 }               | `create`                     |
| `-b` | target blanks                              | { b \| b ∈ N, -1 <= b <= r\^4 } or max ( max = -1 = r\^4 )  | `create`  |
| `-c` | number of sudokus                          | { t \| t ∈ N, t >= 1 }                    | `create`                     |
| `-s` | seed for reproducible results              | { s \| s ∈ Z }                            | `create`, `shuffle`          |
| `-i` | input from a file                          | a .txt file path                          |   `solve`, `shuffle`         |
| `-o` | output to a file                           | a .txt file path                          | `create`, `solve`, `shuffle` |

### Flags without value

| Flag      | Meaning                                                            | Commands                     |
|-----------|--------------------------------------------------------------------|------------------------------|
| `-opuz`   | include the puzzle                                                 | `solve`                      |
| `-osol`   | include the solution                                               | `create`, `shuffle`          |
| `-omin`   | minimal output                                                     | `create`, `solve`, `shuffle` |
| `-oboard` | draw output in a formatted board                                   | `create`, `solve`, `shuffle` |
| `-exact`  | skip the internal search budget (can run very long at high ranks)  | `create`                     |
| `-para`   | spread a batch (count > 1) across CPU cores                        | `create`, `solve`, `shuffle` |

### Examples

```
SudokuGen create
SudokuGen create -r 4 -b 120 -s 777
SudokuGen create -b max -exact
SudokuGen create -c 10 -o ./outputs.txt
SudokuGen solve 390560027.001809405.600172893.180257900.500643000.204081570.003096052.050328619.962010300
SudokuGen solve -i ./inputs.txt -o ./outputs.txt
SudokuGen shuffle 390560027.001809405.600172893.180257900.500643000.204081570.003096052.050328619.962010300 -s 777
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
