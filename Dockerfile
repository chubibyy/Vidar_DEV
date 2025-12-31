FROM ubuntu:22.04

# Avoid prompts from apt
ENV DEBIAN_FRONTEND=noninteractive

# Install dependencies (ca-certificates for SSL, libgtk for AppUI/Unity, libglu1 for OpenGL)
RUN apt-get update && apt-get install -y ca-certificates libgtk-3-0 libglu1-mesa && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /app

# Copy the server build files (You must build for Linux x86_64 first!)
# We expect the build to be in a folder named 'Builds/LinuxServer' relative to this Dockerfile
COPY Builds/LinuxServer/ .

# Make the binary executable (Replace 'Vidar.x86_64' with your actual binary name)
# Assuming standard Unity Linux build naming convention, usually project name or specified in build settings.
# I'll use a generic entrypoint script or assume a name. Let's assume 'VidarServer.x86_64' based on common patterns, 
# but I'll add a comment to rename it.
RUN chmod +x VidarServer.x86_64

# Expose the game port
EXPOSE 7777/udp
EXPOSE 7777/tcp

# Run the server
# -batchmode -nographics are standard. 
# -port 7777 is passed, but our DedicatedServerManager also reads SERVER_PORT env var.
CMD ["./VidarServer.x86_64", "-batchmode", "-nographics", "-mode", "server", "-port", "7777"]
