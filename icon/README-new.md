# Current icon selection

The original main app icon (root ZenQOffice.ico / ZENQOFFICE.png) is active at the user's request. The five *_new application icons remain active. zenqoffice_new.* and zenqoffice_suite_new.png are retained as inactive design concepts, not used by the executable or hub.

# Approved new application icon set

The active source files are writer_new.svg, calc_new.svg, present_new.svg, pdf_new.svg and vector_new.svg. AppIconRenderer loads these files from this directory first, then the matching embedded SVG as fallback. PNG exports and multi-resolution ICO files use the same *_new basename. Standalone application windows prefer the *_new.ico files.

All pre-existing files are retained unchanged. No old icon was overwritten or deleted.

Edit the *_new.svg source files here, then restart the app to refresh its image cache. PNG and ICO exports are snapshots and must be exported again after source changes.

## Main ZenQ Office icon

zenqoffice_new.svg is the editable master logo: a continuous rounded Z ribbon with cyan/blue and violet gradients, subtle contact blur and fine edge highlights. zenqoffice_new.png is its 512px export; zenqoffice_new.ico contains 16, 24, 32, 48, 64, 128 and 256px PNG frames. The executable, hub window, home artwork and newly created main shortcut use the new logo. Original ZENQOFFICE.png, ZenQOffice.ico and older icon folder files are retained unchanged.
