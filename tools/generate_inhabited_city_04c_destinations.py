"""Original civic, hospitality and industrial room authoring for the existing 04C kit.

Shared structural expansion remains in the production C# composition. This module
adds distinct furniture and source data; no Unity placement or transaction is replaced.
"""
import math


def extend_model(m, name, low):
    sides = 8 if low else 16
    if name == 'round_table':
        m.lathe([(0, 0), (.58, 0), (.65, .08), (.35, .18), (.13, .3), (.13, .86), (.4, .94), (0, .94)], sides)
        m.lathe([(0, .91), (.98, .91), (1.04, .96), (1.04, 1.04), (.98, 1.09), (0, 1.09)], sides)
        if not low:
            for a in range(0, 360, 45):
                r = math.radians(a)
                m.rod((math.cos(r)*.2, .3, math.sin(r)*.2), (math.cos(r)*.58, .16, math.sin(r)*.58), .035, sides=6)
    elif name == 'freight_crate':
        m.box((0, .7, 0), (1.7, 1.4, 1.5), .05)
        for x in [-.85, .85]:
            for z in [-.75, .75]:
                m.box((x, .7, z), (.14, 1.5, .14), .025)
        for y in [.1, 1.3]:
            m.box((0, y, -.78), (1.85, .16, .11), .02)
            m.box((0, y, .78), (1.85, .16, .11), .02)
            for x in [-.9, .9]: m.box((x, y, 0), (.1, .16, 1.65), .02)
        for x in [-.55, .55]: m.box((x, .72, -.81), (.08, 1.1, .04), .01)
        m.box((0, .85, -.85), (.3, .25, .1), .025)
        if not low:
            for y in [.32, .58, .84, 1.1]: m.box((0, y, -.755), (1.65, .025, .025), .005)
    elif name == 'engine_stand':
        for x in [-.7, .7]:
            m.box((x, .12, 0), (.22, .24, 1.9), .035)
            m.box((x, .5, .25), (.14, .8, .14), .02)
        m.box((0, .82, .25), (1.75, .18, 1.15), .035)
        m.box((0, 1.15, .25), (1.3, .5, .9), .075)
        for x in [-.72, .72]:
            m.rod((x, .98, -.05), (x, 1.36, .55), .16, sides=sides)
            m.box((x, 1.4, .55), (.3, .15, .28), .025)
        m.lathe([(0, 1.45), (.3, 1.45), (.35, 1.5), (.3, 1.6), (0, 1.6)], sides, center=(0, 0, .25))
        if not low:
            for x in [-.42, 0, .42]: m.box((x, 1.23, -.23), (.06, .3, .08), .012)
    else:
        raise ValueError(name)


def furnish(kind, nodes, anchors, keepouts, lights, labels, node, box, anchor, keepout):
    def add(*a, **kw): nodes.append(node(*a, **kw))
    def B(*a, **kw): nodes.append(box(*a, **kw))
    def at(identifier, position): next(a for a in anchors if a['id'] == identifier)['position'] = list(position)
    def sign(identifier, text, position, size=.07, yaw=0):
        labels.append({'id': identifier, 'text': text, 'position': list(position), 'yaw': yaw, 'characterSize': size})
    names = {'inn': 'CINDER DINER', 'archive': 'RESONANCE ARCHIVE', 'observatory': 'SURVEY OBSERVATORY',
             'garage': 'JAX CALDER CUSTOMS', 'depot': 'RIFT FREIGHT DEPOT'}
    labels[0]['text'] = names[kind]
    # Staff and visitor lanes are inherited from the first route, keeping routine and prompt checks shared.
    if kind in ('inn', 'archive', 'observatory'):
        add('public_service_desk', 'service_counter', 'timber', (4.4, 0, 8))
        add('service_ledger', 'record_stack', 'linen', (4.4, 1.2, 8), solid=False)
    if kind == 'inn':
        for x in [-4.4, 4.4]:
            B('fabric_canopy_'+str(x), 'linen', (x, 3.48, -.55), (4.6, .12, 1.65), False)
            for side in [-1, 1]: B('canopy_support_'+str(x)+str(side), 'timber', (x+side*2.1, 3.03, -.12), (.12, .8, .35), False)
        for index, z in enumerate([8.3, 13.8]):
            add('dining_table_'+str(index), 'round_table', 'timber', (-4.8, 0, z))
            add('table_ceramics_'+str(index), 'pottery_set', 'trim', (-4.8, 1.1, z), (.75, .75, .75), solid=False)
            for side in [-1, 1]: add('dining_chair_'+str(index)+str(side), 'chair_frame', 'timber', (-4.8+side*1.55, 0, z), yaw=-side*90)
        add('courier_window_seat', 'timber_bench', 'timber', (-6.8, 0, 3.6), yaw=90)
        add('hearth_and_cooking_hood', 'ventilation_hood', 'metal', (6.5, 0, 15.5), (.62, .74, .62))
        add('kitchen_workbench', 'service_counter', 'timber', (3.3, 0, 16.2), (.8, 1, .9))
        add('kitchen_crockery', 'pottery_set', 'trim', (3.3, 1.2, 16.2), solid=False)
        add('provision_shelves', 'bookcase', 'timber', (-6.7, 0, 16.1), (.8, 1, 1), yaw=270)
        sign('meal_board', 'MEALS / $40\nASK AT THE COUNTER', (4.4, 2.5, 8.7), .06)
        sign('courier_corner', 'COURIER TABLE\nLOCAL DIRECTIONS', (-6.9, 2.5, 3.8), .055, 90)
    elif kind == 'archive':
        for index, z in enumerate([7.7, 11.2, 15.5]):
            add('reference_shelf_'+str(index), 'bookcase', 'timber', (-7.05, 0, z), yaw=90)
            for row, y in enumerate([.82, 1.4, 1.98]):
                add('shelf_records_'+str(index)+str(row), 'record_stack', 'cloth', (-6.9, y, z), (1.2, .85, 1), yaw=90, solid=False)
        for index, (x, z, prompt) in enumerate([(-4.3, 12.9, (-4.3, 0, 11.2)), (-2.9, 16.3, (-2.9, 0, 14.6)), (3.3, 16.3, (3.3, 0, 14.6))]):
            add('research_table_'+str(index), 'service_counter', 'timber', (x, 0, z), (.64, .9, .75))
            add('readable_record_'+str(index), 'record_stack', 'linen', (x, 1.08, z), solid=False)
            at('record'+str(index+1), prompt)
        add('study_table', 'round_table', 'timber', (-4.6, 0, 8.3), (.85, 1, .85))
        add('study_chair', 'chair_frame', 'timber', (-4.6, 0, 6.65), yaw=0)
        B('display_plinth', 'trim', (6.6, .55, 15.5), (1.2, 1.1, 1.2))
        add('historic_receiver_exhibit', 'crystal_cluster', 'crystal', (6.6, 1.1, 15.5), (.3, .35, .3), source='04A', solid=False)
        B('archive_window_recess_cap', 'trim', (-4.2, 3.4, -.4), (4.3, .2, 1.2), False)
        for x in [-6.2, -2.2]: B('archive_recess_bracket_'+str(x), 'metal', (x, 3.04, -.12), (.15, .65, .4), False)
        sign('record_subjects', 'HOME RECEIVERS / STORM DELIVERY / ALIGNMENT', (0, 3.05, 17.64), .055)
    elif kind == 'observatory':
        add('survey_map_table', 'service_counter', 'timber', (-2.8, 0, 15.8), (.72, .9, .9))
        add('survey_charts', 'record_stack', 'linen', (-2.8, 1.08, 15.8), (1.3, .8, 1.3), solid=False)
        add('ground_waiting_seat', 'timber_bench', 'timber', (-6.8, 0, 3.6), yaw=90)
        for index, (x, z, yaw) in enumerate([(-2.1, 1.4, 180), (2.1, 7.6, 0), (6.1, 3.4, 90)]):
            add('survey_instrument_'+str(index+1), 'survey_scope', 'metal', (x, 4.2, z), (.8, .9, .8), yaw=yaw)
            add('instrument_lens_'+str(index+1), 'crystal_cluster', 'crystal', (x, 6.05, z), (.07, .07, .07), source='04A', solid=False)
        add('upper_research_shelf', 'bookcase', 'timber', (6.6, 4.2, 14.7), yaw=270)
        add('upper_resting_bench', 'timber_bench', 'timber', (2.1, 4.2, 15.6))
        B('observation_balcony', 'trim', (4.2, 4.09, -.5), (4.3, .22, 1.05))
        B('observation_guard', 'metal', (4.2, 5.14, -1.02), (4.3, .08, .08))
        for x in [2.2, 3.2, 4.2, 5.2, 6.2]: B('observation_post_'+str(x), 'metal', (x, 4.68, -1.02), (.07, .96, .07))
        for x in [2.6, 5.8]: B('observation_support_'+str(x), 'trim', (x, 3.6, -.2), (.2, .85, .4))
        lights.append({'id': 'instrument_practical', 'position': [1, 7.35, 7], 'color': [.66, .84, 1], 'range': 11, 'intensity': 1.3})
        B('instrument_room_fixture', 'lamp', (1, 8.12, 7), (.7, .16, .7), False)
        sign('calibration_guide', 'SPLIT CRESCENT / LOW BAND / STEADY FOCUS', (1.8, 7.2, 17.64), .06)
        sign('stair_directions', 'SURVEY INSTRUMENTS\nUPSTAIRS', (-3.9, 2.6, 5.8), .065)
    elif kind == 'garage':
        # A 6.4m opening and unraised floor retain the original vehicle approach.
        keepouts[:] = [k for k in keepouts if k['id'] != 'principal_corridor']
        keepouts.append(keepout('vehicle_bay', (-3.1, 0, -.8), (3.1, 3.2, 14.3)))
        add('parts_shelves', 'bookcase', 'timber', (-6.85, 0, 15.5), yaw=90)
        add('engine_on_service_stand', 'engine_stand', 'metal', (-5.2, 0, 14.8))
        add('mechanic_tool_rack', 'tool_rack', 'metal', (-6.8, 0, 8.2), yaw=90)
        add('pedestrian_service_desk', 'service_counter', 'timber', (5.2, 0, 16.2), (.9, 1, .85))
        add('parts_invoice', 'record_stack', 'linen', (5.2, 1.2, 16.2), solid=False)
        for x in [-2.8, 2.8]: B('bay_edge_stripe_'+str(x), 'trim', (x, .008, 7), (.12, .016, 13.5), False)
        for x in [-7.2, 7.2]:
            B('bay_column_'+str(x), 'metal', (x, 2.2, 9), (.22, 4.4, .22))
            B('work_lamp_'+str(x), 'lamp', (x, 3.8, 9), (.12, .3, 1), False)
        B('bay_overhead_beam', 'metal', (0, 4.45, 9), (14.5, .25, .25))
        at('activity', (4.8, 0, 14.4))
        sign('bay_instructions', 'DRIVE IN / REPAIR AND CUSTOMIZE\nCLEAR BAY TO REVERSE OUT', (0, 3, 17.64), .07)
        sign('pedestrian_instructions', 'VEHICLE SERVICE\nASK AT THE DESK', (5.2, 2.8, 17.64), .06)
    elif kind == 'depot':
        keepouts[:] = [k for k in keepouts if k['id'] != 'principal_corridor']
        keepouts.extend([keepout('public_loading_route', (-1.6, 0, .45), (1.6, 2.2, 14)),
                         keepout('retrieval_cross_aisle', (-5.5, 0, 14.3), (4.4, 2.2, 15.7))])
        # Loading room and secure store, connected by the main aperture and a separate side passage.
        for index, (x, width) in enumerate([(-7.1, 1.2), (-3.1, 1.8), (5, 5.6)]):
            B('freight_partition_'+str(index), 'stone', (x, 2.15, 9), (width, 4.3, .3))
        B('main_store_lintel', 'metal', (0, 3.9, 9), (4.4, .8, .35))
        B('side_store_lintel', 'metal', (-5.2, 3.9, 9), (2.4, .8, .35))
        for index, (x, z) in enumerate([(-6.6, 4.4), (4.5, 5.5), (-6.6, 16.3), (6.6, 14.7)]):
            add('banded_cargo_'+str(index), 'freight_crate', 'timber', (x, 0, z), (.9, 1, .9))
        add('relay_storage_bench', 'service_counter', 'metal', (0, 0, 16.8), (.8, 1, .8))
        add('stored_relay', 'tool_rack', 'metal', (0, 1.2, 16.8), (.25, .25, .25), solid=False)
        add('bypass_console', 'service_counter', 'metal', (-4.7, 0, 6.6), (.6, .95, .7))
        add('bypass_log', 'record_stack', 'linen', (-4.7, 1.14, 6.6), solid=False)
        at('record1', (-2.9, 0, 6.6)); at('activity', (-2.9, 0, 6.6))
        at('defense1', (-4.9, 0, 11.7)); at('defense2', (2.9, 0, 12.1))
        at('retrieve', (0, 0, 15.2))
        for x in [-7.2, 7.2]: B('freight_gantry_post_'+str(x), 'metal', (x, 2.2, 12), (.22, 4.4, .22))
        B('freight_gantry_beam', 'metal', (0, 4.45, 12), (14.5, .22, .3))
        sign('loading_room_name', 'PUBLIC LOADING / OPEN EXIT BEHIND YOU', (0, 3.25, 8.79), .065)
        sign('service_bypass_plate', 'ISOLATE THE FEED\nTHEN RELEASE THE RELAY', (-4.7, 2.5, 7.25), .06)
    else:
        raise ValueError(kind)


def finish(kind, nodes, lights, node, box):
    """Source refinements after the first room-camera review, preserving all clear walking lanes."""
    def B(*a, **kw): nodes.append(box(*a, **kw))
    wall_role = 'plaster' if kind == 'clinic' else 'timber' if kind in ('lofts', 'inn', 'archive') else 'metal'
    for side in [-1, 1]:
        B('wall_lower_panel_'+str(side), wall_role, (side*7.63, .59, 9), (.12, 1.15, 17.6))
        B('wall_chair_rail_'+str(side), 'trim', (side*7.55, 1.2, 9), (.12, .11, 17.6), False)
    # Visible beam ends and crossbeams ground the room scale without descending into headroom.
    beam_y = 3.92 if kind in ('lofts', 'observatory') else 4.72
    for index, z in enumerate([5.5, 11, 16.7]):
        B('supported_ceiling_beam_'+str(index), wall_role, (0, beam_y, z), (15.4, .18, .24), False)
        for side in [-1, 1]:
            B('beam_corbels_'+str(index)+str(side), 'trim', (side*7.43, beam_y-.26, z), (.35, .46, .46), False)
    if kind == 'lofts':
        B('bedroom_left_partition', 'timber', (.5, 6.3, 11.6), (1, 4.2, .2))
        B('bedroom_right_partition', 'timber', (5.45, 6.3, 11.6), (4.5, 4.2, .2))
        B('bedroom_door_lintel', 'timber', (2.1, 7.85, 11.6), (2.2, 1.1, .2))
        B('private_woven_rug', 'linen', (2.1, 4.213, 13.1), (1.8, .026, 2.4), False)
    elif kind in ('clinic', 'forge', 'archive', 'garage', 'depot'):
        position = {'clinic': (-4.7, 2.85, 13), 'forge': (-5.4, 2.1, 13.5), 'archive': (0, 3, 16),
                    'garage': (-5.2, 2.8, 14), 'depot': (0, 3.25, 16)}[kind]
        color = [.76, .9, .83] if kind == 'clinic' else [1, .67, .38] if kind == 'forge' else [1, .83, .6]
        lights.append({'id': 'task_practical', 'position': list(position), 'color': color, 'range': 9, 'intensity': 1.6})
        x, y, z = position
        B('task_light_lens', 'lamp', (x, y+.12, z), (.4, .16, .4), False)
        B('task_light_shade', 'metal', (x, y+.28, z), (.6, .12, .6), False)
        B('task_light_hanger', 'metal', (x, (y+.34+4.8)*.5, z), (.05, 4.8-y-.34, .05), False)
