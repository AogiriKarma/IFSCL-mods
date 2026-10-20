# Traductions

Assembly `Translations`. Trois couches :

- `Row` : un id + six langues (en, fr, it, es, pl, pt). Stocke, ne décide rien.
- `DB` : une table par classe (`Words`, `OptionNames`, `OptionDescr`...), indexée par id, remplie dans le constructeur via `AddRow`.
- `MSG` (IFSCL.Runtime) : applique les règles. Langue courante = `VarG.langage`. Une traduction `""` ou `"-"` retombe sur l'anglais. Id inconnu → `NOTFOUND : id` affiché tel quel, jamais d'exception.

Règle d'Immudelki : l'anglais est obligatoire, le reste optionnel.
