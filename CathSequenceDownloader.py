import pymysql
import json

from bioservices import UniProt
from pymongo import MongoClient


class CathSequenceDownloader:
    cnx = None
    
    def __init__(self, username, password):
        print("Sequence downloader - initialising")
        
        try:
            self.cnx = pymysql.connect(host='10.60.10.162', port=3306,
                                       user=username, password=password,
                                       database='')
            
            self.mongo = MongoClient('mongodb://localhost:27017/')
            self.db = self.mongo['cath-sequence-database']
            self.sequence_collection = self.db['cath-sequences']            
            
            with self.cnx.cursor() as cursor:
                cursor.execute("SELECT @@version")
                row = cursor.fetchone()
                if row:
                    print("Successfully connected to {}.".format(row[0]))            
        except pymysql.Error as e:
            print('Got error {!r}, errno is {}'.format(e, e.args[0]))
            raise Exception("An exception occurred creating the sequence downloader")
        
        print("Sequence downloader - initialisation complete")
        
    def __del__(self):
        try:
            if self.cnx is not None:
                if self.cnx.open:
                    self.cnx.close()
        except Exception as e:
            print('Got error {!r}'.format(e))
            
    def uniprot_sequence_by_protein_id(self, protein_id: str):
        u = UniProt(verbose=False, cache=False)
        sequence = u.retrieve(protein_id, "fasta")

        return sequence    
            
    def get_sequences(self, database = 'mongo'):
        sql_proteins = """
            -- set session group_concat_max_len = 18446744073709;
            SELECT DISTINCT main.unique_id, main.protein_name, 
                            concat('{', GROUP_CONCAT(DISTINCT concat('"', main.cathfunfamilyfull_id, '":', main.funfamregions)), '}') as funfams, 
                            concat('{', GROUP_CONCAT(DISTINCT concat('"', main.cathfamily_id, '":', main.familyregions)), '}') as superfams
            FROM (
                    SELECT DISTINCT p.unique_id, p.protein_name, pcff.cathfunfamilyfull_id, 
                            cff.cathfamily_id, concat('["', replace(pcff.regions, ':', '-'), '"]') as "funfamregions",
                            concat('["', replace(pcf.regions, ':', '-'), '"]') as "familyregions"
                    FROM phd.GoGraph_proteins p
                    JOIN phd.GoGraph_proteincathfunfamily pcff ON p.unique_id = pcff.protein_ref_id
                    JOIN phd.GoGraph_cathfunfamilies cff ON pcff.cathfunfamilyfull_id = cff.cathfunfamilyfull_id
                    JOIN phd.GoGraph_proteincathfamily pcf ON (cff.cathfamily_id = pcf.cathfamily_id and p.unique_id = pcf.protein_ref_id)
                    WHERE p.protein_name IS NOT NULL -- = 'B4NAL6'
            ) main
            group by main.unique_id, main.protein_name;
        """
        
        sql_sequence_insert = """
            INSERT INTO `protein_sequence` (`protein_ref_id`, `sequence`) VALUES (%s, %s)
        """
        
        sql_superfam_regions_insert = """
            INSERT INTO phd.protein_superfamily_region(protein_ref_id, cathfamily_id, region)
            VALUES(%s, %s, %s);
        """
        
        sql_funfam_regions_insert = """
            INSERT INTO phd.protein_cathfunfamily_region(protein_ref_id, cathfunfamilyfull_id, region)
            VALUES(%s, %s, %s);
        """        
        
        with self.cnx.cursor() as cursor:
            cursor.execute(sql_proteins)
            
            fetched_items = 1
            row = cursor.fetchone()
            while row:  # and (fetched_items <= 102):
                sequence = self.uniprot_sequence_by_protein_id(row[1])               
                                
                fixed_superfamily = list(row[3])
                index = 1
                pos = 0
                for c in fixed_superfamily:
                    if c == '-' and (index % 2) == 0:
                        fixed_superfamily[pos] = '","'
                        index = 1
                    else:
                        if (c == '-'):
                            index += 1
                            
                    if c == ']':
                        index = 1  # reset if end of list encountered
                    pos += 1
                
                print(''.join(fixed_superfamily))
                if database == 'mongo':
                    fixed_superfamily = json.loads(''.join(fixed_superfamily).replace("'", "\"").replace('.','_'))  # \uff0E
                else:
                    fixed_superfamily = json.loads(''.join(fixed_superfamily).replace("'", "\""))
                    
                fixed_funfam = list(row[2])
                index = 1
                pos = 0
                for c in fixed_funfam:
                    if c == '-' and (index % 2) == 0:
                        fixed_funfam[pos] = '","'
                        index = 1
                    else:
                        if (c == '-'):
                            index += 1
                            
                    if c == ']':
                        index = 1  # reset if end of list encountered
                    pos += 1
                    
                if database == 'mongo':
                    fixed_funfam = json.loads(''.join(fixed_funfam).replace("'", "\"").replace('.','_'))
                else:    
                    fixed_funfam = json.loads(''.join(fixed_funfam).replace("'", "\""))
                
                if database == 'mysql':                  
                    with self.cnx.cursor() as insert_cursor:
                        insert_cursor.execute(sql_sequence_insert, (row[0], sequence))
                        
                        for key in fixed_superfamily.keys():
                            for region in fixed_superfamily[key]:
                                insert_cursor.execute(sql_superfam_regions_insert,
                                                  (row[0], key, region))
                                
                        for key in fixed_funfam.keys():
                            for region in fixed_funfam[key]:
                                insert_cursor.execute(sql_funfam_regions_insert,
                                                  (row[0], key, region))                        
                
                    self.cnx.commit()
                    print(f"{fetched_items}. {row} updated/inserted.\n{sequence}")
                
                if database == 'mongo':    
                    protein = {
                        "unique_id" : row[0],
                                "protein_name": row[1],
                                "superfamily": fixed_superfamily, 
                                "funfam": fixed_funfam,
                                "sequence": sequence,
                    }
                    
                    protein_filter = {"unique_id": row[0]}
                    
                    sequences = self.db.sequences
                    sequences.replace_one(filter=protein_filter, replacement=protein, upsert = True)               
                    
                    print(f"{fetched_items}. {row} updated/inserted.\n{sequence}")
                
                fetched_items += 1
                row = cursor.fetchone()

def main():
    print("Starting CATH Sequence Data using manual interface")
    
    username = ''
    password = ''
    try:
        opts, args = getopt.getopt(argv,"hu:p:",["help", "username=","password="])
    except getopt.GetoptError:
        print ('CathSequenceDownloader.py -u <username> -p <password>')
        sys.exit(2)
    for opt, arg in opts:
        if opt in ('-h', '--help'):
            print ('CathSequenceDownloader.py -u <username> -p <password>')
            print ('or')
            print ('CathSequenceDownloader.py --username <username> --password <password>')
            sys.exit()
        elif opt in ("-u", "--username"):
            username = arg
        elif opt in ("-p", "--password"):
            password = arg
    
    try:
        sd_downloader = CathSequenceDownloader(username, password)
        sd_downloader.get_sequences(database= 'mysql')
    except Exception as e:
        print('Got error {!r}'.format(e))

if __name__ == '__main__':
    main()
    
