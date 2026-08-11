# HYG bright-star subset notice

`hyg_bright_v41.csv` is a filtered and column-reduced derivative of the HYG Stellar Database v4.1.

- Creator: David Nash / Astronexus
- Source: https://github.com/astronexus/HYG-Database/tree/c7f7f883fe678cc7680169a50ccd7dcc49b060ce/hyg/CURRENT
- Source file: `hygdata_v41.csv`
- Source SHA-256: `d9f69fd86bbf90a4e4d52b4c5c53eacfa6dfcbfdef85bfd94f095e0bebe4ebd`
- Derived file SHA-256: `976abeb38d0d6f7b12fb069943140b59c31a299a72dc04350014e9ca1a4a0e4a`
- Retrieved: 2026-08-11
- License: Creative Commons Attribution-ShareAlike 4.0 International
- License URL: https://creativecommons.org/licenses/by-sa/4.0/

Changes made for Stargazing Hill:

- removed the Solar record and rows without usable position/magnitude values;
- retained stars with visual magnitude `mag <= 6.8`;
- retained only `rarad`, `decrad`, `mag`, and `ci`;
- normalized numeric formatting and line endings.

This derivative dataset is distributed under CC BY-SA 4.0. The Unity mesh baked from it is treated as an adapted database output and is distributed under the same license.
