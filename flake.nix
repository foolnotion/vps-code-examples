{
  description = "vps-dotnet devenv";

  inputs = { nixpkgs.url = "github:nixos/nixpkgs/master"; };

  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      sdk = pkgs.dotnet-sdk_7;
    in rec {
      devShells.${system}.default = (pkgs.buildFHSUserEnv {
        name = "dotnet-fhs";
        runScript = pkgs.writeScript "init.sh" ''
          export DOTNET_ROOT="${sdk}"
          export PATH="$PATH:/home/bogdb/.dotnet/tools:${sdk}"
          exec "bash"
        '';
        targetPkgs = pkgs: [ sdk pkgs.SDL2 pkgs.glfw ];
      }).env;
    };
}
