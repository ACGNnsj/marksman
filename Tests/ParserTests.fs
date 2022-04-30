module Marksman.ParserTests

open Snapper.Attributes
open Xunit

open Parser
open Snapper
open Misc

[<StoreSnapshotsPerClass>]
module SnapshotTests =
    let checkSnapshot (document: array<Element>) =
        let lines =
            Array.map (fun x -> (Element.fmt x).Lines()) document
            |> Array.concat

        lines.ShouldMatchSnapshot()

    let checkInlineSnapshot (document: array<Element>) snapshot =
        let lines =
            Array.map (fun x -> (Element.fmt x).Lines()) document
            |> Array.concat

        lines.ShouldMatchInlineSnapshot(snapshot)

    let scrapeString content = parseText (Text.mkText content)

    [<Fact>]
    let parse_empty () =
        let text = ""
        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parse_title_single () =
        let text = "# Title text"
        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parse_title_multiple () =
        let text =
            "# Title 1\n# Title ... (2)\r\n# 3rd Title"

        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parse_title_with_child_paragraph () =
        let text =
            "# Title 1\nSome text\r\n# Title 2"

        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parse_nested_headings () =
        let text = "# H1 \n## H2.1\n## H2.2\n"
        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parser_link_shortcut_ignore_for_now () =
        let text = "[note]"
        let document = scrapeString text
        checkInlineSnapshot document []

    [<Fact>]
    let parser_xref_note () =
        let text = "[:note]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note]]; (0,0)-(0,7)" ]

    [<Fact>]
    let parser_xref_note_heading_at () =
        //          0123456789012345
        let text = "[:note@heading]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note|heading]]; (0,0)-(0,15)" ]

    [<Fact>]
    let parser_xref_note_heading_pipe () =
        let text = "[:note|heading]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note|heading]]; (0,0)-(0,15)" ]

    [<Fact>]
    let parser_xref_wiki_note () =
        let text = "[[note]]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note]]; (0,0)-(0,8)" ]

    [<Fact>]
    let parser_xref_wiki_note_heading_pipe () =
        //          01234567890123456
        let text = "[[note|heading]]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note|heading]]; (0,0)-(0,16)" ]

    [<Fact>]
    let parser_xref_wiki_text_before () =
        //          0123456789012
        let text = "Before [[N]]"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[N]]; (0,7)-(0,12)" ]

    [<Fact>]
    let parser_xref_wiki_text_after () =
        //          0123456789012345
        let text = "[[note]]! Other"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note]]; (0,0)-(0,8)" ]

    [<Fact>]
    let parser_xref_wiki_text_around () =
        //          0123456789012
        let text = "To [[note]]!"
        let document = scrapeString text
        checkInlineSnapshot document [ "X: [[note]]; (0,3)-(0,11)" ]

    [<Fact>]
    let parser_xref_wiki_2nd_line () =
        //                    1         2         3
        //          0123456789012345678901234567890
        let text = "# H1\nThis is [[note]] huh!\n"
        //          01234 0123456789012345678901
        let document = scrapeString text
        checkSnapshot document

    [<Fact>]
    let parser_completion_point_1 () =
        let text = "[["
        let document = scrapeString text
        checkInlineSnapshot document [ "CP: `[[`: (0,0)-(0,2)" ]

    [<Fact>]
    let parser_completion_point_2 () =
        //          0123456789
        let text = "[[partial other text"
        let document = scrapeString text
        checkInlineSnapshot document [ "CP: `[[partial`: (0,0)-(0,9)" ]

    [<Fact>]
    let parser_completion_point_3 () =
        //          0123456789
        let text = "[[not_cp] other text"
        let document = scrapeString text
        checkInlineSnapshot document []

    [<Fact>]
    let parser_completion_point_4 () =
        //          0123456789
        let text = "P: [:cp other text"
        let document = scrapeString text
        checkInlineSnapshot document [ "CP: `[:cp`: (0,3)-(0,7)" ]

    [<Fact>]
    let parser_completion_point_5 () =
        //          0123456789012
        let text = "P: [par_link other text"
        let document = scrapeString text
        checkInlineSnapshot document [ "CP: `[par_link`: (0,3)-(0,12)" ]

    [<Fact>]
    let complex_example_1 () =
        let text =
            //           1          2          3          4         5
            //1234 5 67890123 4567890123456 789012 34567890123456789012345678901
            "# H1\n\n## H2.1\nP2.1 [:ref1]\n[[cp1\n## H2.2 P2.2 [:cp2 next"
        //   1       2        3             4      5

        let document = scrapeString text
        checkSnapshot document

module XDestTests =
    [<Fact>]
    let parse_at () =
        let actual =
            Markdown.parseXDest "[[foo@bar]]"

        Assert.Equal(XDest.Heading(Some "foo", "bar") |> Some, actual)

    [<Fact>]
    let parse_at_pipe () =
        let actual =
            Markdown.parseXDest "[[foo@bar|baz]]"

        Assert.Equal(XDest.Heading(Some "foo", "bar|baz") |> Some, actual)

    [<Fact>]
    let parse_pipe_at () =
        let actual =
            Markdown.parseXDest "[[foo|bar@baz]]"

        Assert.Equal(XDest.Heading(Some "foo", "bar@baz") |> Some, actual)