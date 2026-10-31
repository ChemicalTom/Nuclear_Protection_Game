# Nuclear Protection — jouer en réseau

Un joueur **héberge** (serveur + joueur), les autres **rejoignent** par IP. Port par défaut : **7777 en UDP**.
2 ou 4 joueurs (équipes équilibrées : 1/1 ou 2/2).

## 1. Même réseau local (le plus simple)
1. L'hôte clique **Héberger**, choisit la durée. Le lobby affiche ses IP locales (ex. `192.168.1.62`).
2. Les autres saisissent cette IP + le port, puis **Rejoindre**.
3. À la première ouverture, Windows demande d'autoriser le jeu dans le pare-feu : accepter (réseau privé).

## 2. À distance : VPN type Tailscale ou ZeroTier (recommandé)
1. Tout le monde installe [Tailscale](https://tailscale.com) (ou ZeroTier) et rejoint le même réseau.
2. L'hôte donne son IP Tailscale (`100.x.y.z`), affichée aussi dans la liste du lobby.
3. Les autres rejoignent avec cette IP. Aucune redirection de port nécessaire.

## 3. À distance : redirection de port (sans VPN)
1. Sur la box de l'hôte, rediriger **UDP 7777** vers l'IP locale du PC hôte.
2. Les autres rejoignent avec l'**IP publique** de l'hôte (voir un site type « what is my ip »).
3. ⚠️ Si le fournisseur d'accès utilise du **CGNAT** (fréquent en 4G/5G et chez certains FAI fibre),
   l'IP publique est partagée et la redirection **ne marchera pas**. Dans ce cas, utiliser un VPN (option 2).

## Tester seul sur un PC
- **Multiplayer Play Mode** (installé) : `Window > Multiplayer > Multiplayer Play Mode`, activer un « Player 2 ».
  Un joueur héberge, l'autre rejoint `127.0.0.1`.
- Ou lancer un build plusieurs fois. Arguments de test utiles :
  `-np-host 7` (héberge 7 min), `-np-join 127.0.0.1`, `-np-name Bob`, `-np-team Tir`, `-np-port 7777`.
- Le bouton Play de l'éditeur démarre toujours depuis la scène `Boot`
  (désactivable : menu `Nuclear Protection > Play depuis Boot`).

## Réglages
Tout est dans `Assets/_Game/Config/GameConfig.asset` (port, durées, nombre de joueurs, etc.).
