/* Strict independent inspection for static L1 art. No Unity import or native physics. */
#include "ufbx.h"
#include <math.h>
#include <stdio.h>
#include <string.h>
int main(int argc, char **argv) {
    if (argc != 2) return 2;
    ufbx_load_opts options = {0}; options.strict = true; options.target_unit_meters = 1;
    ufbx_error error;
    ufbx_scene *scene = ufbx_load_file(argv[1], &options, &error);
    if (!scene) { char text[2048]; ufbx_format_error(text, sizeof(text), &error); fprintf(stderr, "%s\n", text); return 1; }
    int valid = scene->meshes.count == 4 && scene->bones.count == 0 && scene->anim_stacks.count == 0 && scene->lights.count == 0 && scene->cameras.count == 0;
    size_t lod[3] = {0}, proxy = 0;
    printf("{\"scope\":\"Strict independent static FBX inspection, not Unity import\",\"meshes\":[");
    for (size_t m = 0; m < scene->meshes.count; m++) {
        ufbx_mesh *mesh = scene->meshes.data[m];
        if (m) putchar(',');
        printf("{\"name\":\"%s\",\"triangles\":%zu,\"vertices\":%zu}", mesh->name.data, mesh->num_triangles, mesh->num_vertices);
        valid &= mesh->vertex_uv.exists && mesh->vertex_normal.exists && mesh->skin_deformers.count == 0;
        for (size_t i = 0; i < mesh->num_vertices; i++) { ufbx_vec3 p = mesh->vertices.data[i]; valid &= isfinite(p.x) && isfinite(p.y) && isfinite(p.z); }
        if (strstr(mesh->name.data, "_LOD0")) lod[0] = mesh->num_triangles;
        else if (strstr(mesh->name.data, "_LOD1")) lod[1] = mesh->num_triangles;
        else if (strstr(mesh->name.data, "_LOD2")) lod[2] = mesh->num_triangles;
        else if (strstr(mesh->name.data, "_Collision")) proxy = mesh->num_triangles;
        else valid = 0;
    }
    valid &= lod[0] >= 10000 && lod[0] <= 40000 && lod[0] > lod[1] && lod[1] > lod[2] && lod[2] > 0 && proxy > 0 && proxy <= 12000;
    printf("],\"materials\":%zu,\"bones\":%zu,\"clips\":%zu,\"passed\":%s,\"native_unity\":\"NOT RUN\"}\n", scene->materials.count, scene->bones.count, scene->anim_stacks.count, valid ? "true" : "false");
    ufbx_free_scene(scene);
    return valid ? 0 : 1;
}
