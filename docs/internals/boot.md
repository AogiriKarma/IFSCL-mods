# Démarrage du jeu

Point d'entrée : `GamePreInit` (IFSCL.Runtime). Deux vagues de chargement :

1. `AfterAssembliesLoaded` : tables de référence statiques (`XxxList.PRE_LOAD()`).
2. `AfterSceneLoad` → `LoadAll()` : fichiers de contenu en async, seconde vague de `PRE_LOAD`, puis `fullyLoaded = true`.

C'est sur `AfterSceneLoad` que BepInEx est accroché (voir README, entrypoint).
