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
from argparse import ArgumentParser
from typing import List, Any, Callable, Iterable


cube_size = 1.0
half_cube_size = cube_size / 2.0


def main(output_path : str) -> None:
    # uncomment the next line to enable logging to the blender-calling console
    turn_on_loud_logging()
    try:
        # if we're already in object mode this will raise an exception
        bpy.ops.object.mode_set(mode='OBJECT')
    except RuntimeError:
        pass

    logging.info('beginning applying transforms')
    apply_all_transforms()
    save(output_path)
    logging.info('save complete')


def apply_all_transforms():
    world_models = get_all_models()

    logging.info('applying modifiers')
    for model in world_models:
        apply_all_modifiers(model)
        apply_transform(model)

    logging.info('applying transforms')
    for model in world_models:
        apply_transform(model)
        

def select_object(target : bpy.types.Object) -> None:
    bpy.ops.object.select_all(action='DESELECT')
    target.select_set(True)
    bpy.context.view_layer.objects.active = target


def get_objects_in_collection() -> List[bpy.types.Object]:
    root_collection = bpy.context.scene.collection
    return root_collection.all_objects


def get_all_models() -> List[bpy.types.Object]:
    make_all_collections_selectable()
    return get_objects_in_collection()


def make_all_collections_selectable() -> None:
    for view_layer in bpy.context.scene.view_layers:
        recursively_make_collection_visible(view_layer.layer_collection)
            
            
def recursively_make_collection_visible(root_collection : bpy.types.LayerCollection) -> None:
    print(f'{root_collection.name} currently hidden: {root_collection.hide_viewport}')
    root_collection.hide_viewport = False
    
    for child_collection in root_collection.children:
        recursively_make_collection_visible(child_collection)


def apply_all_modifiers(target_object : bpy.types.Object) -> None:
    context_override = bpy.context.copy()
    context_override['object'] = target_object
    modifiers = [modifier for modifier in target_object.modifiers]
    for modifier in modifiers:
        if type(modifier) == bpy.types.ArmatureModifier:
            logging.debug(f'Skipping armature modifier {modifier.name} on {target_object.name}')
        else:
            logging.debug(f'Applying modifier {modifier.name} to {target_object.name}')
            mod_result = bpy.ops.object.modifier_apply(context_override, modifier=modifier.name)
            assert('FINISHED' in mod_result)

    # the armature modifier may still exist
    assert len(target_object.modifiers) <= 1


def apply_transform(target_object : bpy.types.Object) -> None:
    with bpy.context.temp_override(object=target_object):
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    logging.debug(f'Applying transform to {target_object.name}')


def save(output_path : str) -> None:
    logging.info(f'exporting to {output_path}...')

    bpy.ops.wm.save_as_mainfile(filepath=output_path)


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
