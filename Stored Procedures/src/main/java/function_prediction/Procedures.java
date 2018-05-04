package function_prediction;

import java.util.*;
import java.util.stream.*;

import me.xdrop.fuzzywuzzy.model.ExtractedResult;
import org.neo4j.graphdb.GraphDatabaseService;
import org.neo4j.graphdb.index.IndexManager;
import org.neo4j.logging.Log;
import org.neo4j.procedure.*;
import info.debatty.java.stringsimilarity.*;
import me.xdrop.fuzzywuzzy.*;
import static org.neo4j.helpers.collection.MapUtil.stringMap;

public class Procedures {

    // Only static fields and @Context-annotated fields are allowed in
    // Procedure classes. This static field is the configuration we use
    // to create full-text indexes.
    private static final Map<String,String> FULL_TEXT =
            stringMap( IndexManager.PROVIDER, "lucene", "type", "fulltext" );

    // This field declares that we need a GraphDatabaseService
    // as context when any procedure in this class is invoked
    @Context
    public GraphDatabaseService db;

    // This gives us a log instance that outputs messages to the
    // standard log, normally found under `data/log/console.log`
    @Context
    public Log log;

    /**
     * This procedure is used to calculate the Levenshtein string distance between the new sequence and the consensus sequence of the passed node.
     * Step 1 - Generate k-mers of length node consensus. In the case that new sequence is shorter than consensus, find a piece of the consensus that matches new sequence
     * Step 2 - Find the region which matches the consensus in the new sequence or vice-versa
     * Step 3 - Return the highest match
     *
     * @param newSequence - The full sequence that requires classification
     * @param consensus - The consensus sequence of the current cluster being analyzed
     * @param gaps - The number of parsedGaps that are found in the consensus sequence
     * @param threshold - The value that the levenshtein similarity has to exceed for further analysis
     */
    @Procedure(value = "functionPrediction.levenshtein", mode=Mode.READ)
    @Description("This procedure is used to calculate the Levenshtein string distance between the new sequence and the consensus sequence of the passed node")
    public Stream<Match> procedure1( @Name("newSequence") String newSequence, @Name("consensus") String consensus, @Name("parsedGaps") String gaps, @Name("parsedThreshold") String threshold )
    {
        // Some variables
        String source;
        List<String> target;
        boolean reverseComparison = false;
        double levenshteinScore = 0;
        int regionStart = 0;
        int regionLength = 0;
        int parsedGaps = Integer.parseInt(gaps);
        int parsedThreshold = Integer.parseInt(threshold);

        // Check length of functional family consensus
        if(consensus.length() <= newSequence.length()){
            source = consensus;
            target = GenerateKmers(newSequence, source.length());
        }
        else{
            source = newSequence;
            target = GenerateKmers(consensus, source.length());
            reverseComparison = true;
        }

        // Calculate the Levenshtein string distance
        Levenshtein distanceFunction = new Levenshtein();
        int maxLength = source.length();

        // Iterate over all k-mers in the target list
        for (String kmer: target) {

            // Calculate number of parsedGaps if reverse comparison
            if(reverseComparison){
                parsedGaps = NumberOfGaps(kmer);

                // Check if parsedGaps are proportionally placed
                if(parsedGaps >= (kmer.length() / 2)){
                    break;
                }
            }

            // Calculate the distance and overall similarity
            double similarity = distanceFunction.distance(source, kmer);
            double percentage = ((maxLength - (similarity - parsedGaps)) / maxLength) * 100;

            // Check if further analysis is required
            if(percentage >= levenshteinScore){
                levenshteinScore = percentage;
                regionStart = target.indexOf(kmer);
            }
        }

        // If most similar region exceeds parsedThreshold, flag for further analysis
        if(levenshteinScore >= parsedThreshold){
            regionLength = maxLength;
            return Stream.of(new Match((int)levenshteinScore, reverseComparison, regionStart, regionLength));
        }
        else{
            return null;
        }
    }

    /**
     * A method that will generate a list of k-mers for the passed sequence of the passed length
     */
    public List<String> GenerateKmers(String sequence, int length){

        // A list that will hold all k-mers
        List<String> kmers = new ArrayList<String>();

        // Create the k-mers
        for(int i = 0; i <= (sequence.length() - length); i++){
            kmers.add(sequence.substring(i, ((i + length) - 1)));
        }

        // Return the k-mers
        return kmers;
    }

    /**
     * A method that will generate the number of gap characters in a consensus sequence
     */
    public int NumberOfGaps(String sequence){
        int gaps = 0;

        // Iterate over the whole sequence
        for(int i = 0; i < sequence.length(); i++){
            if(sequence.charAt(i) == '-'){
                gaps++;
            }
        }
        return gaps;
    }

    /**
     * This is the output record for our levenshtein procedure. All procedures
     * that return results return them as a Stream of Records, where the
     * records are defined like this one - customized to fit what the procedure
     * is returning.
     */
    public static class Match{
        // Properties
        public long levenshteinSimilarity;
        public boolean reverseComparison;
        public long regionStart;
        public long regionLength;

        // Constructor
        public Match(long levenshteinSimilarity, boolean reverseComparison, long regionStart, long regionLength){
            this.levenshteinSimilarity = levenshteinSimilarity;
            this.reverseComparison = reverseComparison;
            this.regionStart = regionStart;
            this.regionLength = regionLength;
        }
    }
}
