"""Forward to the corrected CMH1 source review. Historical OBJ-context images remain archived."""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name('review_heart_fr01e.py')),run_name='__main__')
