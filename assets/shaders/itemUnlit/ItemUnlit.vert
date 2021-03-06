#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uv;
layout(location = 2) in vec3 normal;

uniform mat4 transform;
out vec2 texCoord;
out vec3 Normal;

void main(void)
{
    gl_Position = vec4(position, 1.0) * transform;
    texCoord = uv;
    Normal = normalize(normal);
}