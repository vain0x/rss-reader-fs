# rss_reader_fs
Simple RSS feeds and tweets viewer.

## Places of interest
- Written in F#
- Saves all data in database (SQLite)
  - Using Entity Framework Code First and [SQLite.CodeFirst](https://github.com/msallin/SQLiteCodeFirst)

## Usage
### Subscribe
Execute the console app and type following commands:

|Command|Effect|
|:------|:------|
|feed *feed-name* *feed-url*|Subscribe a RSS feed.|
|twitter-user *screen-name*|Follow a twitter account. The *screen-name* mustn't begin with `@`.|

### View
Type `show` command or execute the GUI app.

## License
All in this repository which [@vain0](https://github.com/vain0) reserves all rights for are under public domain.
