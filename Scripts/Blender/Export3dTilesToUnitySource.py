#! bpy

import bpy
import bmesh
import mathutils
import math
from collections import namedtuple

import sys
import logging
import pathlib
import os
from pathlib import Path
from argparse import ArgumentParser
from typing import List, Any, Callable, Iterable


ExportableModels = namedtuple('ExportableModels', ['models'])


def main(output_path : str) -> None:
    # uncomment the next line to enable logging to the blender-calling console
    turn_on_loud_logging()
    try:
        # if we're already in object mode this will raise an exception
        bpy.ops.object.mode_set(mode='OBJECT')
    except RuntimeError:
        pass

    make_all_collections_selectable()
    make_all_objects_selectable()

    logging.info('beginning export')
    exportable_models = prepare_for_export()
    export(exportable_models, output_path)
    logging.info('export complete')


def prepare_for_export() -> ExportableModels:
    logging.info('collecting models for export')
    tile_objects = get_tile_objects()
    world_objects = ExportableModels(tile_objects)
    return world_objects


def get_tile_objects() -> bpy.types.Object:
    return bpy.data.collections['Tiles'].all_objects


def select_object(target : bpy.types.Object) -> None:
    bpy.ops.object.select_all(action='DESELECT')
    target.select_set(True)
    bpy.context.view_layer.objects.active = target


def make_all_collections_selectable() -> None:
    for view_layer in bpy.context.scene.view_layers:
        recursively_make_collections_visible(view_layer.layer_collection)


def make_all_objects_selectable() -> None:
    for scene in bpy.data.scenes:
        for obj in scene.objects:
            obj.hide_set(False)
            obj.hide_viewport = obj.hide_render = False
            
            
def recursively_make_collections_visible(root_collection : bpy.types.LayerCollection) -> None:
    print(f'{root_collection.name} currently hidden: {root_collection.hide_viewport}')
    root_collection.hide_viewport = False
    
    for child_collection in root_collection.children:
        recursively_make_collections_visible(child_collection)


def export(models : ExportableModels, output_path : str) -> None:
    exportable_models = models.models
    logging.info(f'exporting to {output_path}...')

    bpy.ops.object.select_all(action='DESELECT')
    for exported_object in exportable_models:
        exported_object.select_set(True)

    bpy.ops.export_scene.fbx(
          filepath=output_path
        , apply_unit_scale=True
        , apply_scale_options='FBX_SCALE_ALL'
        , check_existing=False
        , axis_up='Y'
        , axis_forward='-Z'
        , use_selection=True
        , bake_space_transform=True
        , bake_anim=False
        , bake_anim_use_all_actions=False
    )


def index_of_first(collection : List[Any], pred : Callable[[Any], bool]) -> int:
    for index, value in enumerate(collection):
        if pred(value):
            return index

    return -1


def turn_on_loud_logging():
    logging.getLogger().handlers.clear()
    logging.getLogger().addHandler(logging.StreamHandler())
    logging.getLogger().setLevel(logging.DEBUG)


if __name__ == '__main__':
    parser = ArgumentParser(description='Export a \'.blend\' tile file from art source to Unity source.')
    parser.add_argument('output_path', type=str, help='The path to export to')

    all_blender_args = sys.argv
    arg_separator_index = index_of_first(all_blender_args, lambda arg: arg == '--')
    if arg_separator_index == -1:
        logging.error('Blender was called with no arg separator (\'--\') so there was no way to know where this script\'s arguments began')
        sys.exit(-1)

    script_specific_args = all_blender_args[arg_separator_index + 1:]
    args = parser.parse_args(script_specific_args)
    
    pathlib.Path(args.output_path).parent.mkdir(parents=True, exist_ok=True)
    main(args.output_path)
