# Sudoku Gen Library

`sudoku` `C#` `library` `dll`

&copy; New Media XP  
|
[website](https://www.newmediaxp.com)
|
[email](mailto:contact@newmediaxp.com)
|

---

## Description

A library written in C# to generate and solve sudoku puzzles.

Grids are flat arrays of `squares = rows × rows` ints (`rows = rank²`), row by
row, `0` meaning blank. Rank 2 = 4×4 up to rank 5 = 25×25 (`Sudoku.minRank` /
`Sudoku.maxRank`). Generation cost grows steeply with rank: ranks 2-3 are
instant, rank 4 ≈ 0.1 s, rank 5 ranges from ~18 s (few blanks) to many
minutes (many blanks).

---

## API

```csharp
using NMX.SudokuGen.Library.Core;

// create: rank, desired blanks; unique solution guaranteed
Sudoku a_sudoku = Sudoku.Create(3, 40);

// seeded create: same seed => identical puzzle (e.g. puzzle of the day)
Sudoku a_daily = Sudoku.Create(3, 40, 20260610);

// the grids, read-only; a_sudoku.Removed = actual blank count
IReadOnlyList<int> a_puzz = a_sudoku.Puzzle, a_soln = a_sudoku.Solution;

// solve any puzzle; throws if invalid, duplicate inputs or no unique solution
Sudoku a_solved = Sudoku.Solve(a_puzz);

// shuffle: new equivalent puzzle (same difficulty/blanks, different look),
// derived by segment/row/column permutations, digit relabelling and transpose;
// the original is untouched; seeded overload available
Sudoku a_variant = a_sudoku.Shuffle();

// interactive play: why is the value at index 40 wrong?
Sudoku.Conflict a_why = a_sudoku.FindConflicts(a_board, 40);
// flags: Conflict.Row | Conflict.Column | Conflict.Segment (or None)
// rule-clean but incorrect: a_board[i] != a_sudoku.Solution[i]

// puzzle code: one char per square ('0' blank, '1'-'9', 'A' = 10), '.' between rows
string a_code = Utility.GetPuzzleCode(a_puzz);
int[] a_arr = Utility.GetPuzzleArr(a_code);
```

* Thread safety: no shared mutable state; instances are independent, so
  parallel `Create`/`Solve`/`Shuffle` calls are safe. For background work
  wrap calls in `Task.Run` at the call site.
* Unity: copy the Release DLL into `Assets/Plugins/` and set
  *Player Settings → Api Compatibility Level* to *.NET Standard 2.1*
  (Unity 2021.2+). Pure computation — IL2CPP/AOT safe.

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
