{
  description = "BlueFoxEngine Dev Environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs }:
  let
    system = "x86_64-linux";

    pkgs = import nixpkgs {
      inherit system;
    };
  in
  {
    devShells.${system}.default = pkgs.mkShell {
      packages = with pkgs; [
        dotnet-sdk_10
        raylib

        xorg.libX11
        xorg.libXcursor
        xorg.libXi
        xorg.libXinerama
        xorg.libXrandr
        pipewire
        pulseaudio
        alsa-lib

        mesa
        libGL
      ];

      shellHook = ''
        export LD_LIBRARY_PATH=${
          pkgs.lib.makeLibraryPath [
            pkgs.xorg.libX11
            pkgs.xorg.libXcursor
            pkgs.xorg.libXi
            pkgs.xorg.libXinerama
            pkgs.xorg.libXrandr
            pkgs.mesa
            pkgs.libGL
            pkgs.pipewire
            pkgs.pulseaudio
            pkgs.alsa-lib
          ]
        }

        echo "Ready."
      '';
    };
  };
}
