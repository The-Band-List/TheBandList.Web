## 📑 Sommaire

- [Présentation](#thebandlist-site)
- [Objectifs du projet](#-objectifs-du-projet)
- [Fonctionnalités principales](#fonctionnalités-principales)
    - [Liste des niveaux](#liste-des-niveaux)
    - [Classement des joueurs](#-classement-des-joueurs)
    - [Soumission d’une réussite](#soumission-dune-réussite)
    - [Sécurité & fiabilité](#-sécurité--fiabilité)
- [Interface utilisateur (Site)](#interface-utilisateur-site)
- [Fonctionnalités prévues](#fonctionnalités-prévues-pas-encore-développées)
    - [Améliorations visuelles & UX](#améliorations-visuelles--ux)
    - [Profil utilisateur avancé](#profil-utilisateur-avancé)
    - [Niveaux & musiques](#niveaux--musiques)
    - [Liaison Geometry Dash](#liaison-geometry-dash)
    - [Système de notifications](#-système-de-notifications)
    - [Packs & bonus](#packs--bonus)
- [Technologies utilisées](#technologies)
- [Installation du projet](#installation)

# TheBandList

**TheBandList** est une application web dédiée au jeu **Geometry Dash**.  
Ce projet est développé **par moi**, dans un cadre personnel et par passion.  
Il me permet de continuer à développer mes compétences en développement, aussi bien sur le plan technique que sur la conception d’un projet complet.

L’application permet d’ajouter et de référencer des niveaux de difficulté **Demon**, de suivre les réussites des joueurs et d’afficher un **classement basé sur des réussites validées et vérifiées**.

Le projet met un accent particulier sur :

- l’expérience utilisateur,
- une interface moderne et immersive,
- de bonnes performances,
- la fiabilité des données affichées.

🌐 Site officiel : [https://thebandlist.fr](https://thebandlist.fr)

> ⚠️ Le projet est toujours en cours de développement, mais il est **fonctionnel** et utilisable dans son état actuel.

---

## 🎯 Objectifs du projet

L’objectif principal de TheBandList est de :

- Centraliser une **liste de niveaux Demon** avec leurs informations détaillées
- Permettre aux joueurs de **soumettre leurs réussites**
- Mettre en place un **système de classement** basé sur les points obtenus
- Garantir la **fiabilité des scores** grâce à des preuves vidéo
- Offrir une interface **fluide, claire et immersive**

---

## Fonctionnalités principales

### Liste des niveaux

- Affichage de tous les niveaux référencés
- Informations détaillées :
    - Points attribués
    - Durée du niveau
    - Créateurs
    - Vérificateur
- Intégration de vidéos YouTube (vérification)
- Filtres disponibles :
    - par nom
    - par durée

---

### 🏆 Classement des joueurs

- Classement global basé sur le **total de points**
- Affichage :
    - du nombre de niveaux réussis
    - du nombre de niveaux créés présents dans la liste
- Page profil joueur avec :
    - historique des réussites
- Filtres de classement disponibles :
    - points
    - nombre de niveaux réussis
    - nombre de niveaux créés

---

### Soumission d’une réussite

- Connexion possible via **Discord**
- Formulaire de soumission comprenant :
    - Nom du joueur (si non connecté)
    - Nom du niveau
    - Lien vers la vidéo de preuve
- Validation **manuelle** par l’administration afin de garantir la légitimité de la réussite

---

### 🔒 Sécurité & fiabilité

- Soumissions associées à un compte utilisateur
- Vidéo de preuve obligatoire
- Limitation des doublons et des abus
- Vérification avant validation pour vérifier la légitimité

---

## Interface utilisateur

- Design moderne et sombre
- Navigation claire entre :
    - Liste des niveaux
    - Classement
    - Soumission de réussite
- Mise en valeur visuelle des performances et des records

---

## Fonctionnalités prévues (pas encore développées)

Cette section regroupe les fonctionnalités **en cours de réflexion ou prévues pour les prochaines évolutions** du site.

### Améliorations visuelles & UX

- Refonte complète du **style de la page de classement**
- Création d’un **véritable design de page profil utilisateur**
- Corrections et améliorations du style sur la **liste des niveaux**
- Animation de chargement des images :
    - affichage d’un loader lors du premier chargement
    - apparition de l’image une fois chargée (gestion du cache)

---

### Profil utilisateur avancé

- Interface profil complète avec informations détaillées
- Possibilité pour un utilisateur de **faire une demande de fusion de compte**
- Historique plus détaillé des réussites et statistiques personnelles

---

### Liaison Geometry Dash

- Possibilité de lier un **compte Geometry Dash** à un profil TheBandList
- Affichage automatique des statistiques venant de Geometry Dash :
    - stats globales
    - progression
    - informations publiques du compte
- Eventuellement rajouter dans le classement une possibilité de tri par rapport au stats venant de GeometryDash comme nombre de Demon

---

### 🔔 Système de notifications

- Notifications pour :
    - validation ou refus d’une réussite
    - validation ou refus d'une fusion de compte
    - événements importants du site

---

### Packs & bonus

- Création d’une page dédiée aux **packs**
- Packs permettant d’obtenir des **points bonus**

---

<a id="technologies"></a>

## 🛠️ Technologies utilisées

### 🌐 Site web

- **Frontend**
    - HTML / CSS
    - Blazor
    - JavaScript
- **Backend**
    - C#
    - ASP.NET / Blazor
- **Authentification**
    - Connexion via Discord
- **Intégrations**
    - YouTube (preuves vidéo)

---

<a id="installation"></a>

## ⚙️ Installation du projet

### 1. Cloner le dépôt

```bash
git clone https://github.com/TimeoBlondeleauDubois/TheBandList.git
ToDo
```
