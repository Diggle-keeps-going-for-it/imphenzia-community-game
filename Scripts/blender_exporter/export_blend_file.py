#! python

import os

import subprocess
from pathlib import Path

from blender_exporter.eprint import *
from blender_exporter.path import *
    
    
def apply_all_transforms(blend_file_path_str : str, destination_save_path_str : str) -> None:
    blend_file_path = validate_blender_file(blend_file_path_str)
    process_state = subprocess.run(['blender.exe', '--background', blend_file_path, '--python', str(apply_transform_script_path), '--', destination_save_path_str])
    assert process_state.returncode == 0, 'failed to apply transforms'


def export_blend_3d_tile_file(applied_transform_path_str : str, blend_file_path_str : str) -> None:
    blend_file_path = validate_blender_file(blend_file_path_str)
    applied_transform_path = validate_blender_file(applied_transform_path_str)
    print(f'blend file path: {blend_file_path}, applied_transform_path_str: {applied_transform_path_str}')
    process_state = subprocess.run(['blender.exe', '--background', applied_transform_path_str, '--python', str(blender_tile_export_script_path), '--', calculate_export_path_for_blend_file(blend_file_path)])
    assert process_state.returncode == 0, 'failed to export models'
    
def export_blend_3d_character_file(applied_transform_path_str : str, blend_file_path_str : str) -> None:
    blend_file_path = validate_blender_file(blend_file_path_str)
    applied_transform_path = validate_blender_file(applied_transform_path_str)
    print(f'blend file path: {blend_file_path}, applied_transform_path_str: {applied_transform_path_str}')
    process_state = subprocess.run(['blender.exe', '--background', applied_transform_path_str, '--python', str(blender_character_export_script_path), '--', calculate_export_path_for_blend_file(blend_file_path)])
    assert process_state.returncode == 0, 'failed to export models'
    
def export_blend_3d_prop_file(applied_transform_path_str : str, blend_file_path_str : str) -> None:
    blend_file_path = validate_blender_file(blend_file_path_str)
    applied_transform_path = validate_blender_file(applied_transform_path_str)
    print(f'blend file path: {blend_file_path}, applied_transform_path_str: {applied_transform_path_str}')
    process_state = subprocess.run(['blender.exe', '--background', applied_transform_path_str, '--python', str(blender_prop_export_script_path), '--', calculate_export_path_for_blend_file(blend_file_path)])
    assert process_state.returncode == 0, 'failed to export models'
    
    
def validate_blender_file(file_path : str) -> Path:
    path = Path(file_path)
    if not path.exists():
        raise PathError(f'path "{path}" does not exist')
    if not path.is_file():
        raise PathError(f'path "{path}" is not a file')
    if not path.suffix == '.blend':
        eprint(f'path "{path}" does not have a ".blend" extension, are you sure it\'s a Blender file? Continuing anyway')
    return path


script_directory_path = Path(__file__).parent.parent

relative_path_to_art_source = Path('..') / Path('Art')
art_source_path = (script_directory_path / relative_path_to_art_source).resolve()

relative_path_to_unity_source = Path('..') / Path('Assets')
unity_source_path = (script_directory_path / relative_path_to_unity_source).resolve()

relative_path_to_tile_export_blender_script = Path('Blender') / Path('Export3dTilesToUnitySource.py')
blender_tile_export_script_path = script_directory_path / relative_path_to_tile_export_blender_script

_relative_path_to_character_export_blender_script = Path('Blender') / Path('Export3dCharacterToUnitySource.py')
blender_character_export_script_path = script_directory_path / _relative_path_to_character_export_blender_script

_relative_path_to_prop_export_blender_script = Path('Blender') / Path('Export3dPropsToUnitySource.py')
blender_prop_export_script_path = script_directory_path / _relative_path_to_prop_export_blender_script

relative_path_to_apply_transform_script = Path('Blender') / Path('ApplyTransforms.py')
apply_transform_script_path = script_directory_path / relative_path_to_apply_transform_script


def calculate_export_path_for_blend_file(blend_path : Path) -> Path:
    print(f'calculating path for : {blend_path}')
    relative_path = blend_path.relative_to(art_source_path)
    print(f'calculated path to : {relative_path}')
    return (unity_source_path / relative_path).with_suffix('.fbx')


def calculate_export_folder_path_for_blend_file(blend_path : Path) -> Path:
    relative_path = os.path.basename(blend_path.relative_to(art_source_path))
    return (unity_source_path / relative_path)
