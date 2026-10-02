curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
bash dotnet-install.sh --channel LTS
export PATH="$HOME/.dotnet:$PATH"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc