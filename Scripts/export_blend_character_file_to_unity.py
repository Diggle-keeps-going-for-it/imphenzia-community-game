#! python

from argparse import ArgumentParser
import sys
import os
import tempfile

from blender_exporter.eprint import *
from blender_exporter.path import *
from blender_exporter.export_blend_file import apply_all_transforms, export_blend_3d_character_file


def main(blend_file_path_str : str) -> None:
    temp_dir_path_str = tempfile.mkdtemp()
    temp_file_path_str = os.path.join(temp_dir_path_str, 'applied_transforms.blend')
    apply_all_transforms(blend_file_path_str, temp_file_path_str)
    export_blend_3d_character_file(temp_file_path_str, blend_file_path_str)


if __name__ == '__main__':
    parser = ArgumentParser(description='Export a single \'.blend\' rigged character model from art source to Unity source')
    parser.add_argument('blend_file', type=str, help='The path to the \'.blend\' file to export')
    args = parser.parse_args()

    try:
        main(args.blend_file)
    except Exception as e:
        eprint(str(e))
        sys.exit(1)
