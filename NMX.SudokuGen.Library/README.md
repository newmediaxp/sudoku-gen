# Sudoku Gen Library

`sudoku` `C#` `library` `dll`

&copy; 2024-2026 New Media XP. Licensed under the [MIT License](../LICENSE).  
|
[website](https://www.newmediaxp.com)
|
[email](mailto:contact@newmediaxp.com)
|

---

## Description

A library written in C# to generate and solve sudoku puzzles.

---

## API

```csharp
using NMX.SudokuGen.Library.Core;

Sudoku a_sudoku = Sudoku.Create(3, 40);            // rank 3 (9×9), up to 40 blanks
Sudoku a_solved = Sudoku.Solve(a_sudoku.Puzzle);   // unique solution guaranteed
string a_code   = Utility.GetPuzzleCode(a_sudoku.Puzzle);
```

### `Sudoku`

Immutable puzzle/solution pair. Grids are flat, row-by-row lists of `squares` values, `0` = blank.

#### Methods

| Member          | Signature                                                              | Description                                                                                               |
|-----------------|------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| `Create`        | `static Sudoku Create(int rank, int blanks, bool exhaustive = false)`  | New random puzzle with a unique solution. `blanks = maxBlanks` (-1) requests as many as possible. `exhaustive` skips the search budget for an exact blank count (can run very long at high ranks). |
| `Create`        | `static Sudoku Create(int rank, int blanks, int seed, bool exhaustive = false)` | Seeded create — the same seed reproduces the same puzzle.                                        |
| `Solve`         | `static Sudoku Solve(IReadOnlyList<int> puzzle)`                       | Solves a puzzle, rank inferred from its length. Throws if the input is invalid, conflicting, or has no unique solution. |
| `Shuffle`       | `Sudoku Shuffle()`                                                     | Returns an equivalent variant (same rank, difficulty and blank count, different look). The original is untouched. |
| `Shuffle`       | `Sudoku Shuffle(int seed)`                                            | Seeded shuffle.                                                                                            |
| `FindConflicts` | `Conflict FindConflicts(int[] board, int index)`                      | Which units the value at `index` repeats in. A blank (`0`) never conflicts.                               |

#### Properties

| Member                | Type                 | Description                                                              |
|-----------------------|----------------------|--------------------------------------------------------------------------|
| `Puzzle`              | `IReadOnlyList<int>` | The puzzle grid, `0` = blank.                                            |
| `Solution`            | `IReadOnlyList<int>` | The unique completed solution.                                           |
| `Removed`             | `int`                | Actual blank count (can be less than `blanks` when more would break uniqueness). |
| `rank`                | `int`                | Segment size, 2 (4×4) to 5 (25×25).                                      |
| `rows`                | `int`                | Squares per row, column and segment (`rank²`).                           |
| `squares`             | `int`                | Total squares (`rows²`).                                                 |
| `remove`              | `int`                | Requested blank count (the `blanks` argument to `Create`).               |
| `minRank` / `maxRank` | `const int`          | Allowed rank bounds, `2` and `5`.                                        |
| `maxBlanks`           | `const int`          | Sentinel for `Create`'s `blanks`: `-1` means "as many as possible".      |

### Conflict

`[Flags]` enum, returned by `FindConflicts`.

| Value     | Meaning                                  |
|-----------|------------------------------------------|
| `None`    | No conflict.                             |
| `Row`     | Value repeats in the row.                |
| `Column`  | Value repeats in the column.             |
| `Segment` | Value repeats in the segment (box).      |

A square that is rule-clean but still wrong (`board[i] != Solution[i]`) returns `None` — compare against `Solution` to catch those.

### `Utility`

| Member          | Signature                                              | Description                                                                                                          |
|-----------------|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `GetPuzzleCode` | `static string GetPuzzleCode(IReadOnlyList<int> grid)` | Encodes a grid as a puzzle code: one char per square (`0` blank, `1`-`9`, `A` = 10…), `.` between rows.              |
| `GetPuzzleArr`  | `static int[] GetPuzzleArr(string code)`               | Parses a puzzle code back into a grid array; unrecognised characters are ignored.                                    |

Also exposes general array/bit helpers used internally — `Copy`, `Same`, `Count`, `Swap2`, `Init`, `InitRandom`, `PopCount`.

### Notes

* **Thread safety** — no shared mutable state. Instances are independent, so parallel `Create` / `Solve` / `Shuffle` calls are safe. For background work,
  wrap calls in `Task.Run` at the call site.

---

## Details

* Released on *3 April 2025*

* Dependencies

    * .Net Standard 2.1

* Used in

    * Sudoku Gen Console [(link)](../NMX.SudokuGen.Console)
    * Shaolin Sudou [(link)](https://www.newmediaxp.com/blog/article/shaolin-sudoku)

* Downloads

    * [Github](https://github.com/newmediaxp/sudoku-gen/releases)

---

## Contact

* Link: <https://www.newmediaxp.com/contact>
* Email-1: <contact@newmediaxp.com>
* Email-2: <animaxneil@gmail.com>

---
