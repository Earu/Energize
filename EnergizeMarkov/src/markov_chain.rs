use std::path::Path;
use std::fs;
use std::fs::OpenOptions;
use std::io::BufWriter;
use std::io::BufReader;
use std::io::Write;
use std::io::BufRead;
use rand;
use regex::Regex;
use rand::Rng;
use std::fs::File;

pub struct MarkovChain
{
    path: String,
    max_depth: u32,
    extension: String,
}

impl MarkovChain
{
    pub fn new(path: String,max_depth: u32,extension: String) -> Self
    {
        MarkovChain
        {
            path,
            max_depth,
            extension,
        }
    }

    fn append_all_text(&self,path: String,mut text: String) -> bool
    {
        text.push('\n');
        let fileres = OpenOptions::new()
            .write(true)
            .create(true)
            .append(true)
            .open(path);

        if let Err(_) = fileres
        {
            return false;
        }

        let mut file = fileres.unwrap();
        let mut writer = BufWriter::new(&file);
        writer.write(text.as_bytes());

        return true;
    }

    fn read_all_lines(&self,path: String) -> Vec<String>
    {
        let fileres = File::open(path);
        if let Err(_) = fileres
        {
            return vec![];
        }

        let mut file = fileres.unwrap();
        let mut reader = BufReader::new(&file);
        let mut contents = reader.lines().map(|r| r.unwrap());
        
        return contents.collect::<Vec<String>>();
    }

    fn split<'a>(&self,sentence: &'a str) -> Vec<&'a str>
    {
        let mut re = Regex::new(r"\s|\.|,|!|\?|;|_").unwrap();
        return re.split(sentence).collect::<Vec<&str>>();
    }

    pub fn learn(&self,mut sentence: String) -> bool
    {
        if sentence == ""
        {
            return false;
        }

        if !Path::new(&self.path).exists()
        {
            if let Err(_) = fs::create_dir(&self.path)
            {
                return false;
            }
        }

        sentence = sentence.trim().to_lowercase();
        let mut re = Regex::new(r"[a-z]+://\S+").unwrap();
        sentence = re.replace_all(&sentence,"LINK-REMOVED").to_string();
        sentence = sentence.replace("\\","/").replace("/"," ");
        
        let mut words: Vec<&str> = self.split(&sentence);
        let mut i: i32 = 0;
        let mut worditer = words.iter().peekable();

        while let Some(word) = worditer.next()
        {
            let cur = word.trim();
            let mut next = match worditer.peek()
            {
                Some(x) => x.to_string(),
                None => String::from("END_SEQUENCE")
            };
            next.push('\n');

            let mut path = format!("{}{}{}",self.path,cur,self.extension);
            let mut success = self.append_all_text(path,next.clone());
            if !success { return false; }

            let mut left = format!("{}{}",cur,self.extension);
            for level in 1..self.max_depth
            {
                let depth = i - level as i32;
                if depth >= 0
                {
                    left = format!("{}_{}",words[level as usize].trim(),left);
                    path = format!("{}{}",self.path,left);

                    success = self.append_all_text(path,next.clone());
                    if !success { return false; }
                }
            }
            
            i += 1;
        }

        return true;
    }

    pub fn generate(&self,wordcount: u32) //-> String
    {
        //let mut rng = rand::thread_rng();
    }

    pub fn generate_from(&self,firstpart: String,wordcount: u32) -> String
    {
        let mut rng = rand::thread_rng();
        let mut res = String::new();
        let mut curinput = self.split(&firstpart);

        for _ in 0..wordcount
        {
            let path = format!("{}{}{}",self.path,curinput.join("_"),self.extension);
            if !Path::new(&path).exists()
            {
                let lines = self.read_all_lines(path);
                let index = rng.gen_range::<usize>(0,lines.len());
                let next = lines[index];

                if next == "END_SEQUENCE" { break; }

                res = format!("{} {}",res,next);
                curinput.push(next);
                if curinput.len() > self.max_depth as usize
                {
                    curinput.remove(0);
                }
            }
            else
            {
                break;
            }
        }

        return res;
    }
}