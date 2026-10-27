# Génère src/Resources/Help/GitForce-Guide-FR.pdf
# Usage : pip install --target <dossier> reportlab, puis
#         python tools/MakeGuideFR.py <dossier> src/Resources/Help/GitForce-Guide-FR.pdf
import sys
sys.path.insert(0, sys.argv[1])
out = sys.argv[2]

from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
                                KeepTogether, PageBreak)
from reportlab.graphics.shapes import Drawing, Rect, String
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.fonts import addMapping

pdfmetrics.registerFont(TTFont('Arial', r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('Arial-Bold', r'C:\Windows\Fonts\arialbd.ttf'))
pdfmetrics.registerFont(TTFont('Arial-Italic', r'C:\Windows\Fonts\ariali.ttf'))
pdfmetrics.registerFont(TTFont('Consolas', r'C:\Windows\Fonts\consola.ttf'))
pdfmetrics.registerFont(TTFont('SegoeSym', r'C:\Windows\Fonts\seguisym.ttf'))
addMapping('Arial', 0, 0, 'Arial'); addMapping('Arial', 1, 0, 'Arial-Bold'); addMapping('Arial', 0, 1, 'Arial-Italic'); addMapping('Arial', 1, 1, 'Arial-Bold')

ACCENT = colors.HexColor('#1F5FA8')
ss = getSampleStyleSheet()
body = ParagraphStyle('body', parent=ss['Normal'], fontName='Arial', fontSize=10, leading=14, spaceAfter=5)
h1 = ParagraphStyle('h1', parent=body, fontName='Arial-Bold', fontSize=15, leading=19, textColor=ACCENT, spaceBefore=12, spaceAfter=6)
h2 = ParagraphStyle('h2', parent=body, fontName='Arial-Bold', fontSize=11.5, leading=15, spaceBefore=8, spaceAfter=3)
title = ParagraphStyle('title', parent=body, fontName='Arial-Bold', fontSize=24, leading=30, textColor=ACCENT, spaceAfter=4)
sub = ParagraphStyle('sub', parent=body, fontName='Arial', fontSize=12, textColor=colors.HexColor('#555555'), spaceAfter=14)
bullet = ParagraphStyle('bullet', parent=body, leftIndent=14, bulletIndent=4, spaceAfter=2)
note = ParagraphStyle('note', parent=body, fontName='Arial-Italic', textColor=colors.HexColor('#444444'),
                      backColor=colors.HexColor('#EEF3FA'), borderPadding=6, spaceBefore=4, spaceAfter=8)
cell = ParagraphStyle('cell', parent=body, fontSize=9.5, leading=12.5, spaceAfter=0)
toc = ParagraphStyle('toc', parent=body, leftIndent=8, spaceAfter=1)

def P(t): return Paragraph(t, body)
def H1(t): return Paragraph(t, h1)
def H2(t): return Paragraph(t, h2)
def N(t): return Paragraph(t, note)
def B(items): return [Paragraph(i, bullet, bulletText='\u2022') for i in items]
def K(t): return '<font name="Consolas">%s</font>' % t
def M(t): return '<b>%s</b>' % t.replace('>', '&gt;')

def table(rows, widths):
    data = [[Paragraph(c, cell) for c in r] for r in rows]
    t = Table(data, colWidths=widths, hAlign='LEFT', repeatRows=1)
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#DCE6F2')),
        ('FONTNAME', (0, 0), (-1, 0), 'Arial-Bold'),
        ('GRID', (0, 0), (-1, -1), 0.4, colors.HexColor('#B0B8C4')),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('TOPPADDING', (0, 0), (-1, -1), 3), ('BOTTOMPADDING', (0, 0), (-1, -1), 3),
    ]))
    return t

def layout():
    """Schéma de la fenêtre principale"""
    w, h = 174 * mm, 104 * mm
    d = Drawing(w, h)
    def box(x, y, bw, bh, fill, label, num, label2=None):
        d.add(Rect(x, y, bw, bh, fillColor=colors.HexColor(fill), strokeColor=colors.HexColor('#5A6B80'), strokeWidth=0.8))
        cy = y + bh / 2 - 4 + (5 if label2 else 0)
        d.add(String(x + bw / 2, cy, label, fontName='Arial', fontSize=8.5, textAnchor='middle'))
        if label2:
            d.add(String(x + bw / 2, cy - 11, label2, fontName='Arial', fontSize=7.5, fillColor=colors.HexColor('#555555'), textAnchor='middle'))
        if num:
            d.add(Rect(x + 4, y + bh - 18, 14, 14, fillColor=ACCENT, strokeColor=None))
            d.add(String(x + 11, y + bh - 15, str(num), fontName='Arial-Bold', fontSize=9, fillColor=colors.white, textAnchor='middle'))
    bar = 13
    box(0, h - bar, w, bar, '#E4E8EE', '', None)
    d.add(String(6, h - bar + 3.5, 'Menus : File  Edit  View  Settings  Changelist  Branches  Repository  Remote  Tools  Help   \u2022   barre d\u2019outils',
                 fontName='Arial', fontSize=7.5))
    box(0, h - 2 * bar, w, bar, '#EAF1FB', '', None)
    d.add(String(6, h - 2 * bar + 3.5, 'Barre du d\u00e9p\u00f4t : nom, chemin, branche, d\u00e9p\u00f4t distant, nombre de fichiers modifi\u00e9s',
                 fontName='Arial', fontSize=7.5))
    top = h - 2 * bar
    split = top * 0.40
    left_w = w * 0.30
    mid_w = w * 0.42
    box(0, split, left_w, top - split, '#F7F9FC', 'Arbre / liste des fichiers', 1, 'avec champ de filtre')
    box(left_w, split, mid_w, top - split, '#F2F7EE', 'Onglets', 2, 'Commits \u2022 Revisions \u2022 Graph \u2022 Branches \u2022 Repos \u2022 Log')
    rx = left_w + mid_w
    rh = (top - split)
    box(rx, split + rh * 0.5, w - rx, rh * 0.5, '#FFF8E8', 'Informations du commit', 3, 'Edit Message / Edit Author')
    box(rx, split, w - rx, rh * 0.5, '#FFF8E8', 'Fichiers du commit', None)
    box(0, 0, w, split, '#FDF0F0', 'Diff : version pr\u00e9c\u00e9dente (gauche)  \u2194  version courante (droite)', 4)
    return d

story = []
story.append(Paragraph('GitForce', title))
story.append(Paragraph('Guide de d\u00e9marrage et d\u2019utilisation', sub))
story.append(P('GitForce est une interface graphique pour l\u2019outil en ligne de commande <b>git</b>. '
               'Ce guide d\u00e9crit la fen\u00eatre principale et les op\u00e9rations de cette version : consulter l\u2019\u00e9tat des fichiers, '
               'les indexer, les mettre de c\u00f4t\u00e9 (stash), valider des modifications, parcourir et corriger l\u2019historique, '
               'comparer des versions, g\u00e9rer les branches, les tags et les d\u00e9p\u00f4ts distants.'))
story.append(N('Presque toutes les commandes sont disponibles de plusieurs fa\u00e7ons : par le menu principal, par un clic droit '
               '(menu contextuel), par la barre d\u2019outils et souvent par glisser-d\u00e9poser. Les noms des commandes sont en anglais dans '
               'le programme ; ce guide les cite tels qu\u2019ils apparaissent \u00e0 l\u2019\u00e9cran.'))

story.append(H2('Sommaire'))
for t in ['1. La fen\u00eatre principale', '2. Les fichiers (1)', '3. Les onglets (2)', '4. Le d\u00e9tail d\u2019un commit (3)',
          '5. Le panneau de diff (4)', '6. Valider des modifications', '7. Mettre de c\u00f4t\u00e9 : stash et unstash',
          '8. Branches, fusion et conflits', '9. Corriger l\u2019historique et g\u00e9rer les tags', '10. Historique d\u2019un fichier et blame',
          '11. Le visualiseur de fichiers', '12. D\u00e9p\u00f4ts distants', '13. Commandes Git et ligne de commande',
          '14. Apparence et r\u00e9glages', '15. Raccourcis clavier', '16. Aide et signalement des bugs']:
    story.append(Paragraph(t, toc))

# ---------------------------------------------------------------------------------------------
story.append(PageBreak())
story.append(H1('1. La fen\u00eatre principale'))
story.append(layout())
story.append(Spacer(1, 6))
story += B([
    '<b>Barre du d\u00e9p\u00f4t</b> (sous la barre d\u2019outils) : rappelle le d\u00e9p\u00f4t en cours, son dossier, la branche courante, '
    'le d\u00e9p\u00f4t distant utilis\u00e9 et le nombre de fichiers modifi\u00e9s. Le titre de la fen\u00eatre reprend ces informations.',
    '<b>(1) Fichiers</b> : les fichiers du d\u00e9p\u00f4t, en arbre ou en liste, avec une ic\u00f4ne qui indique leur \u00e9tat git.',
    '<b>(2) Onglets</b> : modifications en attente, historique, graphe, branches, d\u00e9p\u00f4ts et log des commandes git.',
    '<b>(3) D\u00e9tail du commit</b> : en haut les informations du commit choisi dans Revisions ou Graph, en bas la liste de ses fichiers.',
    '<b>(4) Diff</b> : le fichier s\u00e9lectionn\u00e9, version pr\u00e9c\u00e9dente \u00e0 gauche et version courante \u00e0 droite.',
])
story.append(P('Les s\u00e9parateurs entre les panneaux se d\u00e9placent \u00e0 la souris ; leurs positions sont m\u00e9moris\u00e9es. '
               'Avec <b>View &gt; Auto Refresh</b> (activ\u00e9 par d\u00e9faut), les vues se mettent \u00e0 jour d\u00e8s qu\u2019un fichier du d\u00e9p\u00f4t '
               'change, y compris quand la modification vient d\u2019un autre programme ; ' + K('F5') + ' force le rafra\u00eechissement.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('2. Les fichiers (1)'))
story.append(P('Les boutons en haut du panneau basculent entre arbre et liste, rafra\u00eechissent la vue et choisissent le mode d\u2019affichage '
               '(\u00e9galement dans le menu <b>View</b>) :'))
story.append(table([
    ['Mode', 'Ce qui est affich\u00e9'],
    ['Git Status of All Files ' + K('Alt+1'), 'Les fichiers modifi\u00e9s, ajout\u00e9s, supprim\u00e9s et non suivis.'],
    ['Git Status ' + K('Alt+2'), 'La m\u00eame chose, sans les fichiers non suivis.'],
    ['Git View of Repo ' + K('Alt+3'), 'Le contenu du dernier commit, ind\u00e9pendamment des fichiers locaux.'],
    ['Local File View ' + K('Alt+4'), 'Tous les fichiers pr\u00e9sents sur le disque.'],
    ['Local Files Not in Repo ' + K('Alt+5'), 'Les fichiers qui ne sont pas encore dans le d\u00e9p\u00f4t.'],
    ['Local Files Not in .gitignore ' + K('Alt+6'), 'Les fichiers locaux qui ne sont pas exclus par .gitignore.'],
    ['Ignored Files', 'Uniquement les fichiers et dossiers ignor\u00e9s par .gitignore, pour les r\u00e9int\u00e9grer au besoin.'],
    ['Changed Files (flat list)', 'Tous les fichiers suivis modifi\u00e9s, index\u00e9s ou non, en une seule liste avec leur chemin complet. '
     'Les fichiers non suivis n\u2019y figurent pas.'],
], [62 * mm, 112 * mm]))
story.append(Spacer(1, 6))
story.append(P('Le m\u00eame menu trie les fichiers par extension et, avec <b>Show Ignored Files</b>, affiche ou masque les fichiers ignor\u00e9s '
               'dans les autres vues.'))

story.append(H2('Filtrer les fichiers'))
story.append(P('Le champ de filtre, en haut du panneau, ne garde que les fichiers qui correspondent. Plusieurs crit\u00e8res peuvent '
               '\u00eatre combin\u00e9s, s\u00e9par\u00e9s par des espaces ; ' + K('\u00c9chap') + ' ou la croix vide le filtre.'))
story.append(table([
    ['Saisie', 'Effet'],
    [K('*.cs') + ' ou ' + K('.cs'), 'Fichiers de cette extension (les jokers ' + K('*') + ' et ' + K('?') + ' sont admis).'],
    [K('Panel'), 'Fichiers dont le nom ou le chemin contient ce texte.'],
    [K('!test'), 'Exclut ce qui contient ce texte.'],
    [K('*.cs !Designer'), 'Les fichiers C#, sauf ceux des formulaires g\u00e9n\u00e9r\u00e9s.'],
], [50 * mm, 124 * mm]))

story.append(H2('Ic\u00f4nes d\u2019\u00e9tat'))
story.append(table([
    ['Ic\u00f4ne', 'Signification'],
    ['Point d\u2019interrogation', 'Fichier non suivi (untracked) : git ne le conna\u00eet pas encore.'],
    ['Crayon / triangle', 'Fichier modifi\u00e9.'],
    ['Plus', 'Fichier ajout\u00e9 \u00e0 l\u2019index.'],
    ['Croix', 'Fichier supprim\u00e9.'],
    ['Signe d\u2019interdiction rouge, \u00e9l\u00e9ment gris\u00e9', 'Fichier ou dossier ignor\u00e9 par .gitignore.'],
    ['Dossier marqu\u00e9', 'Le dossier contient des modifications.'],
], [62 * mm, 112 * mm]))
story.append(P('Avec <b>View &gt; Toolbar Icons &gt; Modern style</b>, ces ic\u00f4nes sont dessin\u00e9es au trait, adapt\u00e9es aux th\u00e8mes clair et sombre ; '
               'sinon, ce sont les ic\u00f4nes d\u2019origine de GitForce.'))

story.append(H2('Op\u00e9rations sur les fichiers et dossiers'))
story.append(P('S\u00e9lectionnez un ou plusieurs \u00e9l\u00e9ments, puis faites un clic droit (ou utilisez le menu <b>File</b>) :'))
story.append(table([
    ['Commande', 'Effet'],
    ['Stage / Unstage', 'Met dans l\u2019index, ou en retire, toutes les modifications des \u00e9l\u00e9ments s\u00e9lectionn\u00e9s (nouveaux fichiers et '
     'suppressions compris). Les fichiers sur le disque ne changent pas.'],
    ['Ignore (add to .gitignore)', 'Ajoute les fichiers, et les dossiers avec tout leur contenu, \u00e0 .gitignore. Si des fichiers sont d\u00e9j\u00e0 '
     'suivis, GitForce propose de ne plus les suivre en les gardant sur le disque.'],
    ['Ignore *.ext', 'Ignore tous les fichiers ayant l\u2019extension du fichier s\u00e9lectionn\u00e9 (par exemple ' + K('*.bak') + ').'],
    ['Ignore folder', 'Ignore le dossier du fichier s\u00e9lectionn\u00e9, avec tout son contenu.'],
    ['Unignore (make untracked)', 'Retire la r\u00e8gle .gitignore correspondante : l\u2019\u00e9l\u00e9ment redevient non suivi.'],
    ['Untrack (keep on disk)', 'Arr\u00eate le suivi d\u2019un fichier suivi sans le supprimer du disque ; il r\u00e9appara\u00eet comme non suivi (?). '
     'Le retrait devient d\u00e9finitif au prochain commit.'],
    ['Stash Selected...', 'Met de c\u00f4t\u00e9 les modifications des \u00e9l\u00e9ments s\u00e9lectionn\u00e9s (section 7).'],
    ['Blame', 'Pour chaque ligne du fichier, le commit, l\u2019auteur et la date de sa derni\u00e8re modification (section 10).'],
    ['Update Changelist, Revert, Open for Delete, Move/Rename, Remove from File System', 'Les op\u00e9rations habituelles sur les fichiers suivis.'],
    ['Diff vs', 'Compare le fichier \u00e0 l\u2019index (' + K('Ctrl+D') + ') ou au dernier commit (' + K('Ctrl+Shift+D') + ').'],
    ['Edit Using', 'Ouvre le fichier avec l\u2019\u00e9diteur associ\u00e9, un \u00e9diteur de votre choix ou le visualiseur int\u00e9gr\u00e9 (section 11).'],
    ['Revision History...', 'L\u2019historique du fichier avec sa comparaison c\u00f4te \u00e0 c\u00f4te (section 10), ' + K('Ctrl+H') + '.'],
    ['Explore..., Command Prompt...', 'Ouvre le dossier dans l\u2019Explorateur ou une invite de commandes.'],
], [52 * mm, 122 * mm]))
story.append(P('Un clic sur un fichier l\u2019affiche dans le panneau de diff (4). L\u2019action du double-clic se r\u00e8gle dans '
               '<b>Settings &gt; Options</b> : ne rien faire, ouvrir avec l\u2019application associ\u00e9e, avec un programme choisi ou avec le '
               'visualiseur int\u00e9gr\u00e9.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('3. Les onglets (2)'))
story.append(H2('Commits \u2014 modifications en attente ' + K('F6')))
story.append(P('Une <i>changelist</i> regroupe des fichiers pr\u00eats \u00e0 \u00eatre valid\u00e9s. Glissez des fichiers depuis le panneau de gauche vers '
               'une changelist pour les y ajouter. Vous pouvez cr\u00e9er plusieurs changelists, d\u00e9placer des fichiers de l\u2019une \u00e0 l\u2019autre, '
               'puis soumettre chacune s\u00e9par\u00e9ment (<b>Submit...</b>) : GitForce ne valide que les fichiers de la liste soumise. '
               'Un clic sur un fichier l\u2019affiche dans le diff ; le clic droit propose aussi <b>Unstage</b>, <b>Blame</b>, '
               '<b>Revision History</b> et <b>Diff vs Repo HEAD</b>.'))
story.append(H2('Revisions \u2014 historique ' + K('F7')))
story.append(P('L\u2019historique de la branche courante. Un <b>clic</b> sur un commit affiche ses informations et ses fichiers \u00e0 droite (3) ; '
               'un <b>double-clic</b> ouvre <b>Describe Changelist</b>. Le clic droit permet de copier le SHA, de colorer des commits, '
               'de faire un cherry-pick ou un reset, de poser un tag et de modifier le message ou l\u2019auteur d\u2019un commit (section 9).'))
story.append(H2('Graph \u2014 graphe des commits ' + K('F9')))
story.append(P('Les commits sous forme de graphe, comme dans SmartGit ou Tower : chaque branche a sa couleur, les fusions sont reli\u00e9es, '
               'les branches, tags et HEAD sont affich\u00e9s en \u00e9tiquettes. Un bouton choisit entre toutes les branches et la seule branche '
               'courante. Les <b>stashes</b> apparaissent comme des branches sp\u00e9ciales ' + K('stash@{n}') + '. La largeur des colonnes '
               'se r\u00e8gle \u00e0 la souris et est m\u00e9moris\u00e9e.'))
story.append(P('Un clic sur un commit ou un stash affiche ses fichiers \u00e0 droite. Le clic droit permet de basculer sur une branche, de la '
               'fusionner, de la renommer, de cr\u00e9er une branche ou un tag \u00e0 cet endroit, de renommer ou supprimer un tag, de modifier '
               'le message ou l\u2019auteur, de faire un cherry-pick ou un reset (section 9), et, sur un stash, de l\u2019appliquer (<b>Apply</b>), de le r\u00e9cup\u00e9rer (<b>Pop</b>) ou de le supprimer (<b>Drop</b>).'))
story.append(H2('Branches ' + K('F8')))
story.append(P('Les branches locales et distantes. En face de chaque branche : l\u2019identifiant court de son dernier commit, ses tags et le '
               'sujet du commit. Un double-clic sur une branche locale bascule dessus ; le clic droit permet de cr\u00e9er, renommer, '
               'supprimer, basculer et fusionner (section 8).'))
story.append(H2('Repos \u2014 d\u00e9p\u00f4ts ' + K('F10')))
story.append(P('Les d\u00e9p\u00f4ts de l\u2019espace de travail (<i>workspace</i>). Un marqueur indique le d\u00e9p\u00f4t courant, un autre celui qui s\u2019ouvre '
               'par d\u00e9faut. Un double-clic rend un d\u00e9p\u00f4t courant. Le clic droit permet de cr\u00e9er, cloner, rechercher (<b>Scan</b>), '
               'modifier ou retirer des d\u00e9p\u00f4ts et de les regrouper en projets. Les espaces de travail se cr\u00e9ent, se chargent et '
               's\u2019enregistrent depuis le menu <b>File</b>.'))
story.append(H2('Log \u2014 commandes git ' + K('F12')))
story.append(P('Chaque commande git lanc\u00e9e par GitForce s\u2019affiche ici avec son r\u00e9sultat ; les erreurs apparaissent en rouge. '
               'Cet onglet est surtout utile pour comprendre un probl\u00e8me. La ligne de commande (' + K('F3') + ') s\u2019y trouve aussi '
               '(section 13).'))

# ---------------------------------------------------------------------------------------------
story.append(H1('4. Le d\u00e9tail d\u2019un commit (3)'))
story.append(P('Quand un commit est s\u00e9lectionn\u00e9 dans Revisions ou Graph, la partie droite de la fen\u00eatre affiche :'))
story += B([
    '<b>en haut</b>, ses informations : SHA, auteur, date, branches et tags, message complet ; les boutons <b>Edit Message...</b> '
    'et <b>Edit Author...</b> permettent de les corriger (section 9) ;',
    '<b>en bas</b>, la liste de ses fichiers avec leur type de modification. Un clic sur un fichier l\u2019affiche dans le diff (4), '
    'compar\u00e9 \u00e0 sa version dans le commit pr\u00e9c\u00e9dent ; si le fichier n\u2019existait pas avant, la partie gauche reste vide.',
])
story.append(P('Le clic droit sur un fichier de la liste propose :'))
story.append(table([
    ['Commande', 'Effet'],
    ['File History', 'L\u2019historique de ce fichier (section 10).'],
    ['Blame', 'Qui a modifi\u00e9 chaque ligne, dans l\u2019\u00e9tat de ce commit.'],
    ['Open in Built-in Viewer / Open in Editor...', 'Ouvre la version du fichier de ce commit.'],
    ['Open Containing Folder...', 'Ouvre le dossier du fichier dans l\u2019Explorateur.'],
    ['Copy File Path', 'Copie le chemin du fichier.'],
    ['Revert File', 'Annule dans la copie de travail les modifications que ce commit a apport\u00e9es au fichier.'],
    ['Cherry Pick File', 'Applique \u00e0 la copie de travail les modifications de ce commit sur ce seul fichier.'],
    ['Save Patch...', 'Enregistre les modifications du fichier dans ce commit sous forme de patch.'],
], [62 * mm, 112 * mm]))

# ---------------------------------------------------------------------------------------------
story.append(H1('5. Le panneau de diff (4)'))
story.append(P('Le fichier s\u00e9lectionn\u00e9 (dans les fichiers, une changelist ou un commit) s\u2019affiche aussit\u00f4t en entier :'))
story += B([
    '\u00e0 <b>gauche</b>, la version pr\u00e9c\u00e9dente ; \u00e0 <b>droite</b>, la version courante ;',
    'les lignes ajout\u00e9es sont sur fond vert, les lignes supprim\u00e9es sur fond rouge, avec les num\u00e9ros de ligne des deux c\u00f4t\u00e9s ;',
    'pour un fichier sans version pr\u00e9c\u00e9dente (nouveau ou non suivi), la partie gauche reste vide ;',
    '<b>Side by side</b> bascule entre la vue c\u00f4te \u00e0 c\u00f4te et la vue unifi\u00e9e. En c\u00f4te \u00e0 c\u00f4te, les deux versions tiennent dans la '
    'largeur du panneau (la fin des tr\u00e8s longues lignes est coup\u00e9e) ; la vue unifi\u00e9e permet de les faire d\u00e9filer ;',
    '<b>Previous change / Next change</b> (' + K('Shift+F7') + ' / ' + K('F7') + ') passe d\u2019une modification \u00e0 l\u2019autre ; '
    '<b>Refresh</b> recharge les fichiers ; le compteur ' + K('+n -n') + ' r\u00e9sume les lignes ajout\u00e9es et retir\u00e9es ;',
    'le code C#, C, C++, Python, Java, Rust, Go et Pascal est coloris\u00e9 (mots-cl\u00e9s, types, cha\u00eenes, '
    'commentaires, nombres, directives et annotations).',
])
story.append(P('Les commandes <b>Diff vs</b> ouvrent le m\u00eame comparateur dans une fen\u00eatre s\u00e9par\u00e9e. Si un outil externe '
               '(WinMerge, Meld, KDiff3\u2026) est configur\u00e9 dans <b>Settings &gt; Options</b>, ces commandes l\u2019utilisent \u00e0 la place.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('6. Valider des modifications'))
story += B([
    'Indexez les fichiers (<b>Stage</b>, ou glisser-d\u00e9poser vers une changelist), v\u00e9rifiez-les dans le diff, puis <b>Submit...</b> '
    'sur la changelist.',
    'La fen\u00eatre de commit liste les fichiers (\u00e0 cocher ou d\u00e9cocher) et indique la longueur de la premi\u00e8re ligne et du corps du message.',
    '<b>Amend</b> : cochez la case pour remplacer le dernier commit. Son message est repris automatiquement dans la zone de texte, '
    'pr\u00eat \u00e0 \u00eatre gard\u00e9 tel quel ou modifi\u00e9 ; si vous d\u00e9cochez sans l\u2019avoir modifi\u00e9, votre texte pr\u00e9c\u00e9dent revient.',
])
story.append(N('Amend r\u00e9\u00e9crit le dernier commit : \u00e9vitez-le si ce commit a d\u00e9j\u00e0 \u00e9t\u00e9 pouss\u00e9 et que d\u2019autres personnes l\u2019utilisent.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('7. Mettre de c\u00f4t\u00e9 : stash et unstash'))
story.append(P('Le stash met de c\u00f4t\u00e9 des modifications en cours pour les r\u00e9appliquer plus tard, par exemple pour changer de branche.'))
story.append(H2('Stash'))
story.append(P('Par <b>Edit &gt; Stash</b> (tous les fichiers) ou <b>Stash Selected...</b> sur une s\u00e9lection. La fen\u00eatre permet de choisir '
               'les fichiers, de saisir un message et de choisir le mode :'))
story.append(table([
    ['Mode', 'Effet'],
    ['Discard', 'Les modifications sont enregistr\u00e9es puis retir\u00e9es des fichiers de travail (comportement habituel de git stash).'],
    ['Keep index', 'Comme Discard, mais les modifications index\u00e9es restent dans l\u2019index.'],
    ['Keep all', 'Les modifications sont enregistr\u00e9es et restent dans les fichiers : une simple sauvegarde.'],
], [40 * mm, 134 * mm]))
story.append(P('La case <b>Include untracked (new) files</b> inclut les fichiers non suivis.'))
story.append(H2('Unstash'))
story.append(P('<b>Edit &gt; Unstash</b> ouvre la liste des stashes avec, pour chacun, ses fichiers. On peut :'))
story += B([
    '<b>Apply</b> : r\u00e9appliquer le stash en le gardant ; <b>Pop</b> : le r\u00e9appliquer et le supprimer ;',
    '<b>Restore staged state</b> : remettre aussi dans l\u2019index ce qui y \u00e9tait ;',
    '<b>Drop</b> : supprimer un stash ; <b>Edit message...</b> : renommer un stash ;',
    '<b>New branch...</b> : cr\u00e9er une branche \u00e0 partir du stash et l\u2019y appliquer.',
])
story.append(P('Les stashes apparaissent aussi dans l\u2019onglet Graph, o\u00f9 un clic montre leurs fichiers.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('8. Branches, fusion et conflits'))
story += B([
    '<b>Cr\u00e9er</b> : <b>Branches &gt; New...</b>, ou <b>New Branch Here...</b> sur un commit du graphe.',
    '<b>Basculer</b> : double-clic sur une branche, ou <b>Switch to</b>.',
    '<b>Renommer</b> : <b>Rename...</b> dans l\u2019onglet Branches ou dans le graphe.',
    '<b>Fusionner</b> : <b>Merge with...</b>, ou <b>Merge into</b> dans le graphe. La fusion est faite par GitForce lui-m\u00eame, sans outil externe.',
])
story.append(H2('R\u00e9soudre les conflits'))
story.append(P('Quand une fusion produit des conflits, les fichiers concern\u00e9s apparaissent dans l\u2019onglet Commits. <b>Resolve</b> ouvre '
               'le r\u00e9solveur int\u00e9gr\u00e9 : pour chaque conflit, choisissez la version de la branche courante (<b>Ours</b>, ' + K('Alt+1') + '), '
               'celle de la branche fusionn\u00e9e (<b>Theirs</b>, ' + K('Alt+2') + '), les deux dans un ordre ou dans l\u2019autre, ou modifiez le '
               'r\u00e9sultat \u00e0 la main. ' + K('F7') + ' / ' + K('Shift+F7') + ' passent d\u2019un conflit \u00e0 l\u2019autre ; <b>Save and mark resolved</b> '
               'enregistre le fichier. <b>Accept ours</b> / <b>Accept theirs</b> r\u00e8glent un fichier entier d\u2019un coup, et <b>Abort Merge</b> '
               'abandonne la fusion. Terminez par <b>Submit Merge...</b>.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('9. Corriger l\u2019historique et g\u00e9rer les tags'))
story.append(P('Depuis Revisions, Graph ou le d\u00e9tail du commit :'))
story += B([
    '<b>Edit Commit Message...</b> : modifie le message d\u2019un commit, y compris le dernier ;',
    '<b>Edit Commit Author...</b> : modifie le nom et l\u2019e-mail de l\u2019auteur, et au besoin du committer (<b>Also set as committer</b>) ;',
    '<b>New Tag Here...</b>, <b>Rename Tag...</b>, <b>Delete Tag</b> : g\u00e8re les tags, l\u00e9gers ou annot\u00e9s.',
])
story.append(N('Modifier un commit ancien r\u00e9\u00e9crit ce commit et tous ceux qui le suivent : leurs identifiants changent. Si ces commits '
               'ont d\u00e9j\u00e0 \u00e9t\u00e9 pouss\u00e9s, il faudra forcer le push, ce qui g\u00eane les autres personnes qui les utilisent.'))

story.append(H2('Cherry-pick et reset'))
story.append(P('Depuis Revisions ou Graph, sur un commit :'))
story += B([
    '<b>Cherry-Pick...</b> : applique les modifications de ce commit \u00e0 la branche courante, en cr\u00e9ant un nouveau commit ou '
    'seulement dans les fichiers, pour les relire avant de valider. En cas de conflit, le r\u00e9solveur s\u2019ouvre (section 8).',
    '<b>Reset...</b> : ram\u00e8ne la branche courante \u00e0 ce commit, en mode soft, mixed ou hard.',
    '<b>Reset Advanced...</b> : la m\u00eame chose avec les cinq modes de git, chacun expliqu\u00e9 dans la fen\u00eatre :',
])
story.append(table([
    ['Mode', 'Index (fichiers index\u00e9s)', 'Fichiers de travail'],
    ['mixed (par d\u00e9faut)', 'Remis \u00e0 l\u2019\u00e9tat du commit', 'Gardent leurs modifications'],
    ['soft', 'Inchang\u00e9 : il contient toutes les diff\u00e9rences, pr\u00eates \u00e0 \u00eatre valid\u00e9es \u00e0 nouveau', 'Inchang\u00e9s'],
    ['hard', 'Remis \u00e0 l\u2019\u00e9tat du commit', '<b>Remis \u00e0 l\u2019\u00e9tat du commit : les modifications sont perdues</b> '
     '(confirmation demand\u00e9e)'],
    ['keep', 'Remis \u00e0 l\u2019\u00e9tat du commit', 'Gardent leurs modifications ; le reset est annul\u00e9 si un fichier modifi\u00e9 devait \u00eatre remplac\u00e9'],
    ['merge', 'Remis \u00e0 l\u2019\u00e9tat du commit', 'Comme hard, mais le reset est annul\u00e9 si un fichier modifi\u00e9 devait \u00eatre remplac\u00e9'],
], [32 * mm, 62 * mm, 80 * mm]))
story.append(P('En mode hard, <b>Thoroughly fix line endings</b> extrait \u00e0 nouveau tous les fichiers pour leur donner les fins de '
               'ligne demand\u00e9es par .gitattributes.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('10. Historique d\u2019un fichier et blame'))
story.append(H2('Revision History ' + K('Ctrl+H')))
story.append(P('La fen\u00eatre d\u2019historique liste les commits qui ont modifi\u00e9 le fichier, avec la description du commit choisi. '
               '\u00c0 droite, le fichier s\u2019affiche <b>c\u00f4te \u00e0 c\u00f4te</b> :'))
story += B([
    'avec <b>un</b> commit s\u00e9lectionn\u00e9, sa version est compar\u00e9e \u00e0 la version pr\u00e9c\u00e9dente du fichier ; il suffit de parcourir la liste '
    'avec les fl\u00e8ches pour voir l\u2019\u00e9volution du fichier commit apr\u00e8s commit ;',
    'avec <b>deux</b> commits s\u00e9lectionn\u00e9s (' + K('Ctrl') + '+clic), le plus ancien est \u00e0 gauche et le plus r\u00e9cent \u00e0 droite ;',
    '<b>Diff vs. Client file</b> compare une r\u00e9vision au fichier actuel sur le disque ; <b>Diff Revisions</b> compare deux r\u00e9visions '
    'dans une fen\u00eatre s\u00e9par\u00e9e ; <b>View</b> ouvre une r\u00e9vision et <b>Sync</b> remet le fichier dans cet \u00e9tat.',
])
story.append(H2('Blame'))
story.append(P('La fen\u00eatre Blame montre, pour chaque ligne du fichier, le commit, l\u2019auteur et la date de sa derni\u00e8re modification ; '
               'les lignes d\u2019un m\u00eame commit sont regroup\u00e9es. Un double-clic sur une ligne d\u00e9crit le commit correspondant.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('11. Le visualiseur de fichiers'))
story.append(P('Par <b>Edit Using &gt; Built-in Viewer</b>, <b>Open in Built-in Viewer</b> ou le double-clic s\u2019il est r\u00e9gl\u00e9 ainsi, il affiche :'))
story += B([
    'les fichiers texte et le code source avec num\u00e9ros de ligne et colorisation (C#, C, C++, Python, Java, Rust, Go, Pascal), '
    'avec recherche (' + K('Ctrl+F') + ', puis ' + K('F3') + ') ;',
    'les images PNG, JPG, GIF, BMP, ICO et TIF avec zoom ;',
    'pour les autres fichiers binaires, un bouton <b>Open externally</b> qui les ouvre avec l\u2019application associ\u00e9e.',
])

# ---------------------------------------------------------------------------------------------
story.append(H1('12. D\u00e9p\u00f4ts distants'))
story.append(P('Le menu <b>Remote</b> regroupe les \u00e9changes avec les d\u00e9p\u00f4ts distants :'))
story += B([
    '<b>Fetch from Remote</b>, <b>Pull from Remote</b>, <b>Push to Remote</b> : r\u00e9cup\u00e8re ou envoie les commits. Dans la barre d\u2019outils, '
    'maintenir ' + K('Ctrl') + ' en cliquant sur Pull ou Push applique l\u2019op\u00e9ration \u00e0 tous les d\u00e9p\u00f4ts ;',
    '<b>Switch Remote Repo...</b> : choisit le d\u00e9p\u00f4t distant utilis\u00e9 quand il y en a plusieurs ;',
    '<b>Edit Remote Repos...</b> : ajoute, modifie ou supprime des d\u00e9p\u00f4ts distants.',
])
story.append(P('Les cl\u00e9s SSH (<b>Manage SSH Keys...</b>) et les mots de passe HTTPS (<b>Manage HTTPS Pass...</b>) se g\u00e8rent dans le menu <b>Settings</b>.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('13. Commandes Git et ligne de commande'))
story.append(H2('Fen\u00eatre Commandes Git ' + K('Ctrl+Shift+G')))
story.append(P('<b>Tools &gt; Git Commands...</b> ouvre un aide-m\u00e9moire de plus de 120 commandes git class\u00e9es par th\u00e8me '
               '(création et clonage, état, index, commits, historique, branches et fusion, dépôts distants, tags, stash, '
               'annulation, réécriture avancée de l’historique, sous-modules et worktrees, recherche d’un bug avec bisect, '
               'configuration, maintenance), '
               'chacune avec une courte explication, pr\u00e9sent\u00e9es en accord\u00e9on. On peut aussi les ex\u00e9cuter directement :'))
story += B([
    'le champ du haut filtre les commandes (par exemple ' + K('stash') + ' ou ' + K('annuler') + ') ;',
    'un clic met la commande dans la ligne ' + K('git') + ', avec la premi\u00e8re ' + K('&lt;valeur&gt;') + ' s\u00e9lectionn\u00e9e pour \u00eatre remplac\u00e9e ; '
    'un double-clic ou ' + K('Entr\u00e9e') + ' l\u2019ex\u00e9cute dans le d\u00e9p\u00f4t courant et la sortie s\u2019affiche \u00e0 droite ;',
    '<b>Arr\u00eater</b> interrompt une commande en cours, <b>Effacer</b> vide la sortie, les fl\u00e8ches ' + K('\u2191') + ' / ' + K('\u2193') +
    ' rappellent les commandes d\u00e9j\u00e0 lanc\u00e9es ;',
    'les commandes marqu\u00e9es <font name="SegoeSym">\u26a0</font> peuvent perdre des modifications ou r\u00e9\u00e9crire l\u2019historique : GitForce demande confirmation ;',
    'apr\u00e8s une commande qui modifie le d\u00e9p\u00f4t, la fen\u00eatre principale se met \u00e0 jour.',
])
story.append(H2('Ligne de commande ' + K('F3')))
story.append(P('<b>View &gt; Command Line</b> affiche une ligne de commande dans l\u2019onglet Log, pour taper une commande git sans quitter '
               'la fen\u00eatre principale.'))

# ---------------------------------------------------------------------------------------------
story.append(H1('14. Apparence et r\u00e9glages'))
story += B([
    '<b>View &gt; Theme</b> : th\u00e8me clair, sombre ou celui du syst\u00e8me ; GitForce propose de red\u00e9marrer pour l\u2019appliquer.',
    '<b>View &gt; Toolbar Icons</b> : ic\u00f4nes petites (16), moyennes (24) ou grandes (32), et <b>Modern style</b> pour les ic\u00f4nes au trait.',
    '<b>View &gt; Auto Refresh</b> : rafra\u00eechissement automatique quand des fichiers changent.',
    '<b>Settings &gt; Options</b> : chemin de git, outils de diff et de merge, \u00e9diteurs, action du double-clic, '
    'nombre de commits affich\u00e9s, polices, etc.',
    '<b>Tools</b> : ajoutez vos propres outils (<b>Customize</b>), import\u00e9s ou export\u00e9s avec <b>Import</b> / <b>Export</b> ; ils '
    'apparaissent aussi dans les menus contextuels.',
])

# ---------------------------------------------------------------------------------------------
story.append(H1('15. Raccourcis clavier'))
story.append(table([
    ['Touche', 'Action'],
    [K('F5'), 'Rafra\u00eechir'],
    [K('F6') + ' / ' + K('F7') + ' / ' + K('F8') + ' / ' + K('F9') + ' / ' + K('F10') + ' / ' + K('F12'),
     'Onglets Commits / Revisions / Branches / Graph / Repos / Log'],
    [K('F3'), 'Afficher ou masquer la ligne de commande'],
    [K('Alt+1') + ' \u2026 ' + K('Alt+6'), 'Modes d\u2019affichage des fichiers'],
    [K('Ctrl+A'), 'Tout s\u00e9lectionner'],
    [K('Ctrl+D') + ' / ' + K('Ctrl+Shift+D'), 'Diff avec l\u2019index / avec le dernier commit'],
    [K('Ctrl+H'), 'Historique du fichier'],
    [K('Ctrl+Enter'), '\u00c9diter avec l\u2019\u00e9diteur associ\u00e9'],
    [K('Ctrl+Shift+G'), 'Fen\u00eatre Commandes Git'],
    [K('F7') + ' / ' + K('Shift+F7'), 'Dans un diff : modification suivante / pr\u00e9c\u00e9dente ; dans le r\u00e9solveur : conflit suivant / pr\u00e9c\u00e9dent'],
    [K('Alt+1') + ' / ' + K('Alt+2'), 'Dans le r\u00e9solveur de conflits : garder Ours / Theirs'],
    [K('\u00c9chap'), 'Dans le champ de filtre : vider le filtre'],
], [58 * mm, 116 * mm]))

# ---------------------------------------------------------------------------------------------
story.append(H1('16. Aide et signalement des bugs'))
story += B([
    '<b>Help &gt; Guide de d\u00e9marrage (fran\u00e7ais)</b> : ce guide.',
    '<b>Help &gt; Report a Bug / Suggestion</b> : ouvre la page des issues du projet sur GitHub pour signaler un probl\u00e8me ou proposer une am\u00e9lioration.',
    '<b>Help &gt; Check for Updates</b> : v\u00e9rifie s\u2019il existe une nouvelle version ; <b>Help &gt; About</b> : version, auteurs et lien vers les issues.',
    '<b>Help &gt; Home</b> et <b>Getting Started</b> : la documentation d\u2019origine de GitForce, en anglais.',
])
story.append(Spacer(1, 8))
story.append(N('GitForce a \u00e9t\u00e9 \u00e9crit par Goran Devic et est distribu\u00e9 sous licence GPLv3. Cette version a \u00e9t\u00e9 modifi\u00e9e par '
               'Germain Philippe : https://github.com/philgerm57/GitForce'))

def footer(canvas, doc):
    canvas.saveState()
    canvas.setFont('Arial', 8)
    canvas.setFillColor(colors.HexColor('#777777'))
    canvas.drawString(18 * mm, 10 * mm, 'GitForce \u2014 Guide de d\u00e9marrage')
    canvas.drawRightString(A4[0] - 18 * mm, 10 * mm, 'Page %d' % doc.page)
    canvas.restoreState()

doc = SimpleDocTemplate(out, pagesize=A4, leftMargin=18 * mm, rightMargin=18 * mm, topMargin=16 * mm, bottomMargin=18 * mm,
                        title='GitForce - Guide de d\u00e9marrage', author='GitForce', lang='fr')
doc.build(story, onFirstPage=footer, onLaterPages=footer)
print('ok', out)
