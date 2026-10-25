# Translations

Assembly `Translations`. Three layers:

- `Row`: one id plus six languages (en, fr, it, es, pl, pt). Stores, decides nothing.
- `DB`: one table per class (`Words`, `OptionNames`, `OptionDescr`...), indexed by id, filled in the constructor through `AddRow`.
- `MSG` (IFSCL.Runtime): applies the rules. Current language is `VarG.langage`. A translation equal to `""` or `"-"` falls back to English. Unknown id → `NOTFOUND : id` displayed as is, never an exception.

Immudelki's rule: English is mandatory, the rest is optional.
